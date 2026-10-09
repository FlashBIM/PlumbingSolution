"""
Vẽ ảnh preview Connect Pipe theo style Revit (ống xám, phụ kiện hồng, viền đen mảnh, nền xám nhạt).
Ray-cast giải tích (hình trụ có nắp + capsule) với camera chiếu song song (isometric), khử răng cưa 3x.

python3 render_previews.py <thư mục ra>
"""
import math
import os
import sys

import numpy as np
from PIL import Image

W, H = 480, 320          # ảnh ra
SS = 3                   # siêu lấy mẫu
BG = np.array([0.957, 0.957, 0.957])
PIPE = np.array([0.70, 0.70, 0.71])
FIT = np.array([0.96, 0.66, 0.68])
EDGE = np.array([0.10, 0.10, 0.10])

R_MAIN = 0.5
R_BR = 0.5
K_FIT = 1.14            # bán kính thân phụ kiện / bán kính ống
K_SLV = 1.26            # măng sông ở miệng phụ kiện
BEND = 1.6              # bán kính uốn co / bán kính ống


def nz(v):
    v = np.asarray(v, float)
    return v / np.linalg.norm(v)


class Scene:
    def __init__(self):
        self.prims = []   # (kind, a, b, r, color, id)
        self.nid = 0

    def new_id(self):
        self.nid += 1
        return self.nid

    def cyl(self, a, b, r, color, oid=None):
        oid = oid or self.new_id()
        self.prims.append(('cyl', np.asarray(a, float), np.asarray(b, float), r, color, oid))
        return oid

    def cap(self, a, b, r, color, oid):
        self.prims.append(('cap', np.asarray(a, float), np.asarray(b, float), r, color, oid))

    def pipe(self, a, b, r=R_BR):
        return self.cyl(a, b, r, PIPE)

    def sleeve(self, p, d, r, oid):
        d = nz(d)
        L = 0.42 * r
        self.cyl(p - d * L, p, r * K_SLV, FIT, oid)

    def tee(self, c, main_dir, out_dir, r_main=R_MAIN, r_out=R_BR):
        """Tê: thân dọc ống chính, nhánh ra theo out_dir; trả về điểm miệng nhánh."""
        oid = self.new_id()
        md, od = nz(main_dir), nz(out_dir)
        c = np.asarray(c, float)
        half = 1.45 * r_main
        self.cyl(c - md * half, c + md * half, r_main * K_FIT, FIT, oid)
        tip = c + od * (r_main * 1.55)
        self.cyl(c, tip, r_out * K_FIT, FIT, oid)
        self.sleeve(c + md * half, md, r_main, oid)
        self.sleeve(c - md * half, -md, r_main, oid)
        self.sleeve(tip, od, r_out, oid)
        return tip

    def elbow(self, p, din, dout, r=R_BR):
        """Co tại góc p: ống đến theo din, đi ra theo dout. Trả về (điểm tiếp tuyến vào, ra)."""
        oid = self.new_id()
        a, b = nz(din), nz(dout)
        theta = math.acos(max(-1.0, min(1.0, float(np.dot(a, b)))))
        R = BEND * r
        t = R * math.tan(theta / 2)
        p = np.asarray(p, float)
        t0, t1 = p - a * t, p + b * t
        # cung tròn qua t0 -> t1, tâm ở phía trong góc
        nrm = nz(b - a * float(np.dot(a, b)))  # hướng vào trong góc uốn
        center = t0 + nrm * R
        u0 = t0 - center
        u1 = t1 - center
        n = 10
        pts = []
        for i in range(n + 1):
            s = i / n
            # slerp giữa u0 và u1
            ang = theta * s
            axis = nz(np.cross(u0, u1))
            v = (u0 * math.cos(ang) + np.cross(axis, u0) * math.sin(ang) + axis * np.dot(axis, u0) * (1 - math.cos(ang)))
            pts.append(center + v)
        for i in range(n):
            self.cap(pts[i], pts[i + 1], r * K_FIT, FIT, oid)
        # măng sông 2 đầu
        self.cyl(t0 - a * 0.42 * r, t0 + a * 0.05 * r, r * K_SLV, FIT, oid)
        self.cyl(t1 - b * 0.05 * r, t1 + b * 0.42 * r, r * K_SLV, FIT, oid)
        return t0, t1


def render(scene, path, view=(1.0, -1.35, 0.95)):
    vd = -nz(view)                 # hướng tia
    up = np.array([0, 0, 1.0])
    right = nz(np.cross(vd, up))
    cup = nz(np.cross(right, vd))

    # khung nhìn vừa với cảnh
    pts = []
    for k, a, b, r, *_ in scene.prims:
        for p in (a, b):
            for dx in (-r, r):
                pts.append(p + right * dx)
                pts.append(p + cup * dx)
    pts = np.array(pts)
    xs, ys = pts @ right, pts @ cup
    cx, cy = (xs.min() + xs.max()) / 2, (ys.min() + ys.max()) / 2
    span = max((xs.max() - xs.min()) / W, (ys.max() - ys.min()) / H) * 1.18
    w, h = W * SS, H * SS
    px = (np.arange(w) + 0.5 - w / 2) * span / SS + cx
    py = (h / 2 - np.arange(h) - 0.5) * span / SS + cy
    PX, PY = np.meshgrid(px, py)
    ro = PX[..., None] * right + PY[..., None] * cup - vd * 1000.0
    ro = ro.reshape(-1, 3)
    rd = vd

    N = ro.shape[0]
    tbest = np.full(N, np.inf)
    nbest = np.zeros((N, 3))
    cbest = np.tile(BG, (N, 1))
    idbest = np.zeros(N, int)

    for kind, a, b, r, color, oid in scene.prims:
        ba = b - a
        oc = ro - a
        baba = ba @ ba
        bard = ba @ rd
        baoc = oc @ ba
        k2 = baba - bard * bard
        k1 = baba * (oc @ rd) - baoc * bard
        k0 = baba * np.einsum('ij,ij->i', oc, oc) - baoc * baoc - r * r * baba
        hh = k1 * k1 - k2 * k0
        ok = hh >= 0
        sq = np.sqrt(np.where(ok, hh, 0))
        t = np.where(ok, (-k1 - sq) / (k2 if abs(k2) > 1e-12 else 1e-12), np.inf)
        y = baoc + t * bard
        body = ok & (y > 0) & (y < baba)
        tt = np.where(body, t, np.inf)
        nrm = np.zeros((N, 3))
        hit = ro + tt[:, None] * rd
        if kind == 'cyl':
            nb = (oc + t[:, None] * rd - ba[None, :] * (y / baba)[:, None]) / r
            nrm[body] = nb[body]
            if abs(bard) > 1e-12:
                tc = ((np.where(y < 0, 0.0, baba)) - baoc) / bard
                capok = ok & ~body & (np.abs(k1 + k2 * tc) < sq)
                tt = np.where(capok, tc, tt)
                nc = np.outer(np.sign(y), ba / math.sqrt(baba))
                nrm[capok] = nc[capok]
        else:
            # capsule: phần đầu tròn
            rest = ok & ~body
            for end in (a, b):
                oe = ro - end
                bb = oe @ rd
                cc = np.einsum('ij,ij->i', oe, oe) - r * r
                h2 = bb * bb - cc
                okk = h2 > 0
                ts = np.where(okk, -bb - np.sqrt(np.where(okk, h2, 0)), np.inf)
                use = okk & (ts < tt)
                tt = np.where(use, ts, tt)
            hit = ro + tt[:, None] * rd
            ap = hit - a
            s = np.clip((ap @ ba) / baba, 0, 1)
            closest = a + s[:, None] * ba
            nrm = (hit - closest) / r
        better = tt < tbest
        tbest = np.where(better, tt, tbest)
        nbest[better] = nrm[better]
        cbest[better] = color
        idbest[better] = oid

    hitm = np.isfinite(tbest)
    L1 = nz((0.35, -0.55, 0.85))
    L2 = nz((-0.6, 0.3, 0.4))
    nn = nbest / np.maximum(np.linalg.norm(nbest, axis=1, keepdims=True), 1e-9)
    dif = np.clip(nn @ L1, 0, 1) * 0.55 + np.clip(nn @ L2, 0, 1) * 0.18
    shade = 0.42 + dif
    spec = np.clip(nn @ nz(L1 - vd), 0, 1) ** 28 * 0.25
    col = np.where(hitm[:, None], np.clip(cbest * shade[:, None] + spec[:, None], 0, 1), BG)

    img = col.reshape(h, w, 3)
    ids = idbest.reshape(h, w)
    dep = np.where(hitm, tbest, 1e9).reshape(h, w)
    nr = nn.reshape(h, w, 3)

    # viền: đổi đối tượng, nhảy sâu, hoặc gãy pháp tuyến (mép nắp ống)
    edge = np.zeros((h, w), bool)
    for dy, dx in ((0, 1), (1, 0), (1, 1), (1, -1)):
        i2 = np.roll(np.roll(ids, dy, 0), dx, 1)
        d2 = np.roll(np.roll(dep, dy, 0), dx, 1)
        n2 = np.roll(np.roll(nr, dy, 0), dx, 1)
        e = (ids != i2) | (np.abs(dep - d2) > span * 4) | ((np.einsum('ijk,ijk->ij', nr, n2) < 0.5) & (ids > 0))
        edge |= e
    # dày ~1px ở ảnh ra
    k = SS // 2
    thick = edge.copy()
    for dy in range(-k, k + 1):
        for dx in range(-k, k + 1):
            if dy * dy + dx * dx <= k * k:
                thick |= np.roll(np.roll(edge, dy, 0), dx, 1)
    img[thick] = img[thick] * 0.15 + EDGE * 0.85

    out = img.reshape(H, SS, W, SS, 3).mean(axis=(1, 3))
    Image.fromarray((out * 255 + 0.5).astype(np.uint8)).save(path)


# ------------------------------------------------------------------ các cảnh
X, Y, Z = np.eye(3)
L = 5.0       # nửa chiều dài ống chính
DZ = 3.4      # chênh cao
BL = 5.0      # chiều dài ống nhánh vẽ


def main_through(s, x0=-L, x1=L):
    s.pipe((x0, 0, 0), (x1, 0, 0), R_MAIN)


def branch_e45(s):
    main_through(s)
    tip = s.tee((0, 0, 0), X, nz(Y + Z))
    p = np.array([0, DZ, DZ])
    t0, t1 = s.elbow(p, nz(Y + Z), Y)
    s.pipe(tip, t0)
    s.pipe(t1, p + Y * BL)


def branch_e90(s):
    main_through(s)
    tip = s.tee((0, 0, 0), X, Z)
    p = np.array([0, 0, DZ])
    t0, t1 = s.elbow(p, Z, Y)
    s.pipe(tip, t0)
    s.pipe(t1, p + Y * BL)


def branch_same(s):
    main_through(s)
    tip = s.tee((0, 0, 0), X, Y)
    s.pipe(tip, (0, BL + 1, 0))


def elbow_e45(s):
    p0 = np.array([0, 0, 0.0])
    d = nz(Y + Z)
    t0, t1 = s.elbow(p0, X, d)
    s.pipe((-2 * L, 0, 0), t0, R_MAIN)
    p = np.array([0, DZ, DZ])
    u0, u1 = s.elbow(p, d, Y)
    s.pipe(t1, u0)
    s.pipe(u1, p + Y * BL)


def elbow_e90(s):
    p0 = np.array([0, 0, 0.0])
    t0, t1 = s.elbow(p0, X, Z)
    s.pipe((-2 * L, 0, 0), t0, R_MAIN)
    p = np.array([0, 0, DZ])
    u0, u1 = s.elbow(p, Z, Y)
    s.pipe(t1, u0)
    s.pipe(u1, p + Y * BL)


def elbow_same(s):
    p0 = np.array([0, 0, 0.0])
    t0, t1 = s.elbow(p0, X, Y)
    s.pipe((-2 * L, 0, 0), t0, R_MAIN)
    s.pipe(t1, (0, BL + 1, 0))


GAP = 7.0


def parallel_e45(s):
    main_through(s)
    tip = s.tee((0, 0, 0), X, nz(Y + Z))
    q = np.array([0, GAP, DZ])            # đầu tự do của ống song song
    a0, a1 = s.elbow(q, -X, -Y)           # co 90 trên mặt bằng
    s.pipe(q + X * L, a0)
    p = np.array([0, DZ, DZ])
    b0, b1 = s.elbow(p, -Y, -nz(Y + Z))   # co 45
    s.pipe(a1, b0)
    s.pipe(b1, tip)


def parallel_e90(s):
    main_through(s)
    tip = s.tee((0, 0, 0), X, Z)
    q = np.array([0, GAP, DZ])
    a0, a1 = s.elbow(q, -X, -Y)
    s.pipe(q + X * L, a0)
    p = np.array([0, 0, DZ])
    b0, b1 = s.elbow(p, -Y, -Z)
    s.pipe(a1, b0)
    s.pipe(b1, tip)


def parallel_same_h(s):
    main_through(s)
    tip = s.tee((0, 0, 0), X, Y)
    q = np.array([0, GAP, 0])
    a0, a1 = s.elbow(q, -X, -Y)
    s.pipe(q + X * L, a0)
    s.pipe(a1, tip)


def parallel_same_v(s):
    main_through(s)
    tip = s.tee((0, 0, 0), X, Z)
    q = np.array([0, 0, DZ + 0.6])
    a0, a1 = s.elbow(q, -X, -Z)
    s.pipe(q + X * L, a0)
    s.pipe(a1, tip)


SCENES = {
    'ConnectPipe_Branch_Elbow45': branch_e45,
    'ConnectPipe_Branch_Elbow90': branch_e90,
    'ConnectPipe_Branch_SameElevation': branch_same,
    'ConnectPipe_Elbow_Elbow45': elbow_e45,
    'ConnectPipe_Elbow_Elbow90': elbow_e90,
    'ConnectPipe_Elbow_SameElevation': elbow_same,
    'ConnectPipe_Parallel_Elbow45': parallel_e45,
    'ConnectPipe_Parallel_Elbow90': parallel_e90,
    'ConnectPipe_Parallel_SameElevation_Horizontal': parallel_same_h,
    'ConnectPipe_Parallel_SameElevation_Vertical': parallel_same_v,
}

if __name__ == '__main__':
    out = sys.argv[1]
    only = sys.argv[2:]
    os.makedirs(out, exist_ok=True)
    for name, fn in SCENES.items():
        if only and name not in only:
            continue
        s = Scene()
        fn(s)
        render(s, os.path.join(out, name + '.png'))
        print('saved', name)
