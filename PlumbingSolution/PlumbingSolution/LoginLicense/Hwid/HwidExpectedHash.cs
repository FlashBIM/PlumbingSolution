// SHA-256 của hwid.dll mà add-in được phép nạp. HwidInterop từ chối nạp nếu file trên
// đĩa không khớp giá trị này — chặn một hwid.dll giả cùng ABI biến một license key
// thành vô hạn máy trong khi mọi kiểm tra mật mã phía sau vẫn khớp hoàn hảo.
//
// ⚠ ĐỔI hwid.dll thì PHẢI cập nhật giá trị này, nếu không add-in sẽ từ chối nạp.
//    Tính lại bằng:  sha256sum PlumbingSolution/native/hwid.dll
//
// ⚠ GIỚI HẠN THỰC TẾ: giá trị này chỉ có sức nặng khi DLL add-in được bảo vệ. Bản
//    PlumbingSolution hiện KHÔNG qua bước obfuscate nào — kẻ thay được hwid.dll cũng sửa
//    được hằng số này bằng trình soạn hex. Nó ngăn được tai nạn và kẻ tấn công qua
//    đường, không ngăn được người quyết tâm.

namespace PlumbingSolution.LoginLicense.Hwid
{
    public static partial class HwidInterop
    {
        private const string ExpectedSha256 =
            "fc1472534102164337004c7235ed023653d0e36a5c4e95df1b748c1ad41ae0b4";
    }
}
