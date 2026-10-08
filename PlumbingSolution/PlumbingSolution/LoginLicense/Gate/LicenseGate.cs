using System;
using System.Threading;
using PlumbingSolution.LoginLicense.Verify;

namespace PlumbingSolution.LoginLicense.Gate
{
    /// <summary>
    /// Cổng license duy nhất của add-in. Mọi lệnh gọi hàm này ở đầu <c>Execute</c>:
    ///
    /// <code>if (!LicenseGate.IsToolAllowed(this)) return Result.Cancelled;</code>
    ///
    /// VÌ SAO CẦN, khi ribbon đã bị làm mờ: nút mờ chỉ là giao diện. Lệnh Revit vẫn gọi
    /// được bằng phím tắt, bằng Dynamo, bằng macro. Ribbon là trải nghiệm người dùng —
    /// hàm này mới là ranh giới bảo mật thật.
    ///
    /// Quyết định được tính TẠI THỜI ĐIỂM CHẠY LỆNH từ <see cref="LicenseState"/> đã xác
    /// thực, không đọc cờ đã cache. Kẻ lạ có gọi <see cref="Bind"/> với một state tự tạo
    /// thì state đó cũng RỖNG, và tool bị khoá — hỏng theo hướng an toàn.
    /// </summary>
    public static class LicenseGate
    {
        private const string FeatureAll = "all";

        private static LicenseState _state;
        private static Action _onBlocked;
        private static long _lastGraceLogUtcTicks;

        /// <summary>Nối trạng thái license của phiên vào cổng. Gọi một lần lúc khởi động.</summary>
        public static void Bind(LicenseState state)
        {
            Bind(state, null);
        }

        /// <summary>
        /// Như trên, kèm hành động chạy khi một lệnh bị chặn. Chỗ này để hiện thông báo
        /// và đánh thức vòng nhịp — một cú nhấp bị chặn KHÔNG được im lặng, người dùng
        /// sẽ tưởng add-in hỏng.
        /// </summary>
        public static void Bind(LicenseState state, Action onBlocked)
        {
            _state = state;
            _onBlocked = onBlocked;
        }

        /// <summary>
        /// Có license hợp lệ, HOẶC đang trong vùng đệm sau khi token hết hạn tự nhiên.
        /// Phải CÙNG luật với <see cref="IsToolAllowed(string)"/> — hai luật khác nhau
        /// cho hai lối vào là cái bẫy chắc chắn có ngày sập.
        /// </summary>
        public static bool HasValidLicense
        {
            get { return ComputeAllowed(null, true); }
        }

        /// <summary>
        /// Lệnh có được phép chạy không. Mã lệnh lấy tự động từ tên kiểu đầy đủ nên không
        /// phải điền tay ở từng lệnh.
        /// </summary>
        public static bool IsToolAllowed(object tool)
        {
            return IsToolAllowed(ToolId(tool));
        }

        public static bool IsToolAllowed(string toolId)
        {
            bool allowed = ComputeAllowed(toolId, false);
            if (!allowed)
            {
                Action onBlocked = _onBlocked;
                if (onBlocked != null)
                {
                    try
                    {
                        onBlocked();
                    }
                    catch (Exception)
                    {
                        // Thông báo hỏng không được biến thành lỗi của chính lệnh đó.
                    }
                }
            }

            return allowed;
        }

        /// <param name="ignoreFeature">
        /// true khi chỉ hỏi "có license hay không" (bật/tắt cả nhóm ribbon), không xét
        /// từng tính năng.
        /// </param>
        private static bool ComputeAllowed(string toolId, bool ignoreFeature)
        {
            LicenseState s = _state;
            if (s == null)
            {
                return false;
            }

            bool isActive = s.IsActive;
            bool inGrace = !isActive && s.IsWithinGrace;
            if (!isActive && !inGrace)
            {
                return false;
            }

            if (inGrace)
            {
                LogGraceUsageThrottled();
            }

            VerifiedLicense license = s.Current;
            if (license == null)
            {
                return false;
            }

            if (ignoreFeature || license.HasAllFeatures)
            {
                return true;
            }

            if (string.IsNullOrEmpty(toolId))
            {
                return false;
            }

            // Server hiện luôn trả "all"; nhánh theo từng tính năng để dành cho việc bán
            // theo tính năng sau này mà không phải đổi hợp đồng.
            for (int i = 0; i < license.Features.Count; i++)
            {
                string feature = license.Features[i];
                if (string.Equals(feature, FeatureAll, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(feature, toolId, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        // Cổng này được gọi ở đầu MỌI lệnh nên nhật ký phải thưa: một dòng mỗi 30 giây là
        // đủ để dựng lại chuỗi sự kiện khi khách gửi log về.
        private static void LogGraceUsageThrottled()
        {
            long nowTicks = DateTime.UtcNow.Ticks;
            long lastTicks = Interlocked.Read(ref _lastGraceLogUtcTicks);
            if (nowTicks - lastTicks < TimeSpan.TicksPerSecond * 30)
            {
                return;
            }

            Interlocked.Exchange(ref _lastGraceLogUtcTicks, nowTicks);
            LicenseLog.Write("gate: đang chạy nhờ vùng đệm (token hết hạn, đang chờ nối lại)");
        }

        /// <summary>Mã lệnh ổn định = tên kiểu đầy đủ.</summary>
        public static string ToolId(object tool)
        {
            return tool != null ? tool.GetType().FullName : string.Empty;
        }
    }
}
