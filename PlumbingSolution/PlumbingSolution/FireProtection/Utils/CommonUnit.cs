using Autodesk.Revit.DB;
using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace PlumbingSolution.FireProtection.Ultis
{
    /// <summary>
    /// Hàm đơn vị mm / feet-inch ghép từ Common của Quick MEP (form Vertical Pipe dùng):
    /// project đơn vị imperial thì offset nhập feet-inch, size hiện inch phân số.
    /// </summary>
    public static partial class Common
    {
        public static void ChangeLengthUnitFeetInchToMilimeter(System.Windows.Forms.TextBox textBox, string unit)
        {
            if (unit == Define.UnitInch)
            {
                string value = textBox.Text;
                ChangeUnitFeetInchToMilimeter(textBox, value);
            }
        }

        public static void ChangeUnitFeetInchToMilimeter(System.Windows.Forms.TextBox tb, string valueInInchString)
        {
            double valueinMM = FormatFeetInchFractionToMillimeter(valueInInchString);
            if (!double.IsNaN(valueinMM))
            {
                tb.Text = valueinMM.ToString();
            }
        }

        public static void ChangeUnitMilimeterToFeetInch(System.Windows.Forms.TextBox tb, string valueInMMString)
        {
            if (double.TryParse(valueInMMString, out double valueinMM))
            {
                tb.Text = FormatMillimeterToFeetInchFraction(valueinMM);
            }
        }
        public static string ChangeUnitMilimeterToFeetInch(string valueInMMString)
        {
            if (double.TryParse(valueInMMString, out double valueinMM))
            {
                return FormatMillimeterToFeetInchFraction(valueinMM);
            }

            return valueInMMString;
        }

        private static double EvaluateMixedFraction(string wholePart, string fractionPart)
        {
            double whole = double.TryParse(wholePart, out double w) ? w : 0;

            var frac = fractionPart.Split('/');
            if (frac.Length == 2 &&
                double.TryParse(frac[0], out double numerator) &&
                double.TryParse(frac[1], out double denominator) &&
                denominator != 0)
            {
                return whole + (numerator / denominator);
            }

            return whole;
        }

        public static double FormatFeetInchFractionToMillimeter(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return double.NaN;

            input = input.Trim();
            bool isNegative = input.StartsWith("-");
            if (isNegative) input = input.Substring(1).Trim();

            double feet = 0;
            double inch = 0;

            // Tìm vị trí dấu '
            int indexFeet = input.IndexOf('\'');
            if (indexFeet >= 0)
            {
                string feetPart = input.Substring(0, indexFeet).Trim();
                double.TryParse(feetPart, out feet);
                input = input.Substring(indexFeet + 1).Trim();
            }

            // Bỏ dấu " cuối (nếu có)
            if (input.EndsWith("\""))
                input = input.Substring(0, input.Length - 1).Trim();

            // Tách phần inch còn lại (có thể là: số nguyên, phân số, hoặc cả hai)
            string[] inchParts = input.Split(' ');
            foreach (var part in inchParts)
            {
                if (part.Contains("/"))
                {
                    var frac = part.Split('/');
                    if (frac.Length == 2 &&
                        double.TryParse(frac[0], out double numerator) &&
                        double.TryParse(frac[1], out double denominator) &&
                        denominator != 0)
                    {
                        inch += numerator / denominator;
                    }
                }
                else if (double.TryParse(part, out double whole))
                {
                    inch += whole;
                }
            }

            double totalInch = feet * 12 + inch;
            double mm = totalInch * 25.4;
            return isNegative ? -mm : mm;
        }

        public static string FormatFeetToFeetInchFraction(double totalFeet)
        {
            bool isNegative = totalFeet < 0;
            totalFeet = Math.Abs(totalFeet);

            double totalInch = totalFeet * 12;

            int feet = (int)(totalInch / 12);
            double inchDecimal = totalInch - feet * 12;

            int wholeInch = (int)Math.Floor(inchDecimal);
            double fractionalPart = inchDecimal - wholeInch;

            const int denominator = 256; // Theo chuẩn Revit
            int numerator = (int)Math.Round(fractionalPart * denominator);

            if (numerator == denominator)
            {
                numerator = 0;
                wholeInch += 1;
            }

            if (wholeInch == 12)
            {
                wholeInch = 0;
                feet += 1;
            }

            string inchStr;

            if (numerator == 0)
            {
                inchStr = $"{wholeInch}\"";
            }
            else
            {
                string frac = ReduceFraction(numerator, denominator);
                inchStr = wholeInch > 0 ? $"{wholeInch} {frac}\"" : $"0 {frac}\"";
            }

            string result = $"{feet}' {inchStr}".Trim();
            return isNegative ? "-" + result : result;
        }

        public static double FormatFractionInchToMilimeter(string input)
        {
            double totalInch = 0;

            try
            {
                var tokens = input.Replace("\"", "").Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                foreach (var token in tokens)
                {
                    if (token.Contains("/"))
                    {
                        var parts = token.Split('/');
                        if (parts.Length == 2 &&
                            double.TryParse(parts[0], out double num) &&
                            double.TryParse(parts[1], out double den) &&
                            den != 0)
                        {
                            totalInch += num / den;
                        }
                    }
                    else if (double.TryParse(token, out double val))
                    {
                        totalInch += val;
                    }
                }
            }
            catch (Exception)
            {
            }

            return totalInch * 25.4;
        }

        private static double FormatInchOnly(string input)
        {
            var tokens = input.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            double inch = 0;

            foreach (var token in tokens)
            {
                if (token.Contains("/"))
                {
                    var parts = token.Split('/');
                    if (parts.Length == 2 &&
                        double.TryParse(parts[0], out double num) &&
                        double.TryParse(parts[1], out double den) &&
                        den != 0)
                    {
                        inch += num / den;
                    }
                }
                else if (double.TryParse(token, out double inchPart))
                {
                    inch += inchPart;
                }
            }

            return inch;
        }

        public static string FormatMilimeterToFractionInch(double mm)
        {
            const int denominator = 256;
            double totalInches = mm / 25.4;

            int wholeInches = (int)Math.Floor(totalInches);
            double fractionalPart = totalInches - wholeInches;

            int numerator = (int)Math.Round(fractionalPart * denominator);

            if (numerator == denominator)
            {
                numerator = 0;
                wholeInches += 1;
            }

            string result;

            if (wholeInches == 0 && numerator > 0)
            {
                // Khi nhỏ hơn 1 inch, hiển thị fraction
                result = $"{ReduceFraction(numerator, denominator)}\"";
            }
            else if (numerator == 0)
            {
                // Inch tròn
                result = $"{wholeInches}\"";
            }
            else
            {
                // Có cả inch nguyên và fraction
                result = $"{wholeInches} {ReduceFraction(numerator, denominator)}\"";
            }

            return result;
        }

        public static string FormatMillimeterToFeetInchFraction(double mm)
        {
            double feet = mm / 304.8;
            return FormatFeetToFeetInchFraction(feet);
        }

        private static int GCD(int a, int b)
        {
            while (b != 0)
            {
                int temp = b;
                b = a % b;
                a = temp;
            }
            return a;
        }

        public static string GetUnit()
        {
            var doc = Global.UIDoc.Document;

#if DEBUG2020 || DEBUG2021 || RELEASE2020 || RELEASE2021 || RELEASE_2020 || RELEASE_2021 || Bundle_2020 || Bundle_2021 || BUNDLE_2020
            return Define.UnitMM;
#else
            FormatOptions formatOptions = doc.GetUnits().GetFormatOptions(SpecTypeId.Length);

            if (IsMetricUnitType(formatOptions.GetUnitTypeId()))
                return Define.UnitMM;
            else
                return Define.UnitInch;
#endif
        }

        private static bool IsFraction(string input)
        {
            return Regex.IsMatch(input, @"^\d+/\d+$");
        }

        private static bool IsMetricUnitType(ForgeTypeId displayUnitType)
        {
            if (displayUnitType == UnitTypeId.Centimeters ||
                displayUnitType == UnitTypeId.Millimeters ||
                displayUnitType == UnitTypeId.Decimeters ||
                displayUnitType == UnitTypeId.Meters ||
                displayUnitType == UnitTypeId.MetersCentimeters)
            {
                return true;
            }

            return false;
        }

        private static bool IsMixedFraction(string[] tokens)
        {
            if (tokens.Length != 2)
                return false;

            bool firstValid = double.TryParse(tokens[0], out _);
            bool secondValid = Regex.IsMatch(tokens[1], @"^\d+/\d+$");

            return firstValid && secondValid;
        }

        private static char PeekChar(string text, int index)
        {
            return (index >= 0 && index < text.Length) ? text[index] : '\0';
        }

        private static string ReduceFraction(int numerator, int denominator)
        {
            int gcd = GCD(numerator, denominator);
            numerator /= gcd;
            denominator /= gcd;
            return $"{numerator}/{denominator}";
        }

        public static void SettingLengthUnitToFeetInch(System.Windows.Forms.TextBox textBox, string unit)
        {
            if (unit == Define.UnitInch)
            {
                string value = textBox.Text;
                ChangeUnitMilimeterToFeetInch(textBox, value);
            }
        }

        public static void TextBoxCanInputFeetInch_KeyPress(object sender, KeyPressEventArgs e, bool allowNegativeValue = false, bool isInch = true)
        {
            var textBox = sender as System.Windows.Forms.TextBox;
            string text = textBox.Text;
            int selectionStart = textBox.SelectionStart;

            if (!isInch)
            {
                // Chế độ nhập số thực đơn vị mm hoặc giá trị thuần
                bool isControl = char.IsControl(e.KeyChar);
                bool isDigit = char.IsDigit(e.KeyChar);
                bool isDot = e.KeyChar == '.';
                bool isMinus = e.KeyChar == '-';

                if (!isControl && !isDigit && !isDot && (!allowNegativeValue || !isMinus))
                {
                    e.Handled = true;
                    return;
                }

                if (isDot && text.Contains("."))
                {
                    e.Handled = true;
                    return;
                }

                if (isMinus && (text.Contains("-") || selectionStart != 0))
                {
                    e.Handled = true;
                    return;
                }

                return;
            }

            // Chế độ isInch = true: cho nhập feet, inch, phân số
            if (char.IsControl(e.KeyChar) || char.IsDigit(e.KeyChar))
                return;

            // Không cho nhập gì sau dấu inch (")
            int inchIndex = text.IndexOf('"');
            if (inchIndex >= 0 && selectionStart > inchIndex && !char.IsControl(e.KeyChar))
            {
                e.Handled = true;
                return;
            }

            if (e.KeyChar == ' ')
            {
                if (selectionStart == 0)
                {
                    e.Handled = true;
                    return;
                }

                char prevChar = PeekChar(text, selectionStart - 1);

                // Không cho nhập space nếu đứng sau dấu inch (")
                if (text.Contains("\"") && selectionStart > text.IndexOf('\"'))
                {
                    e.Handled = true;
                    return;
                }

                // Không cho nhập space sau phân số (ví dụ 1/2)
                if (prevChar == '/' || char.IsDigit(prevChar) && PeekChar(text, selectionStart - 2) == '/')
                {
                    e.Handled = true;
                    return;
                }

                // Cho phép space sau số, dấu ', "
                if (!char.IsDigit(prevChar) && prevChar != '/' && prevChar != '\'' && prevChar != '\"')
                {
                    e.Handled = true;
                }

                return;
            }

            if (e.KeyChar == '.')
            {
                var blocks = text.Split('\'', '"');
                foreach (var part in blocks)
                {
                    if (part.Contains(".") && selectionStart <= text.IndexOf(part) + part.Length)
                    {
                        e.Handled = true;
                        return;
                    }
                }
                return;
            }

            if (e.KeyChar == '/')
            {
                if (selectionStart == 0 || !char.IsDigit(PeekChar(text, selectionStart - 1)))
                    e.Handled = true;
                return;
            }

            if (e.KeyChar == '-')
            {
                if (!allowNegativeValue || selectionStart != 0 || text.Contains("-"))
                    e.Handled = true;
                return;
            }

            if (e.KeyChar == '\'')
            {
                if (text.Contains("'") || selectionStart == 0 || !char.IsDigit(PeekChar(text, selectionStart - 1)))
                    e.Handled = true;
                return;
            }

            if (e.KeyChar == '\"')
            {
                if (text.Contains("\""))
                {
                    e.Handled = true;
                    return;
                }

                if (selectionStart == 0 || (!char.IsDigit(PeekChar(text, selectionStart - 1)) && PeekChar(text, selectionStart - 1) != '/'))
                    e.Handled = true;
                return;
            }

            e.Handled = true;
        }

        public static void TextBoxCanInputFeetInch_Leave(object sender, EventArgs e, string unit)
        {
            if (unit != Define.UnitInch)
                return;

            var textBox = sender as System.Windows.Forms.TextBox;
            if (textBox == null)
                return;

            string input = textBox.Text.Trim();
            if (string.IsNullOrEmpty(input))
                return;

            try
            {
                string feetPart = "";
                string inchPart = "";

                bool hasFeet = input.Contains("'");
                bool hasInch = input.Contains("\"");

                // 👉 Xử lý âm: nếu có dấu âm đầu chuỗi hoặc bất kỳ số âm nào trong input
                bool isNegative = input.StartsWith("-") || Regex.IsMatch(input, @"(^|\s)-\d");

                if (input.StartsWith("-"))
                    input = input.Substring(1).Trim();

                if (!hasFeet && !hasInch)
                {
                    // Trường hợp đặc biệt: 2 block trở lên → block đầu là feet, còn lại là inch
                    var tokens = input.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                    if (tokens.Length == 1)
                    {
                        // chỉ 1 số → coi là feet
                        if (double.TryParse(tokens[0], out double feetOnly))
                        {
                            if (isNegative)
                                feetOnly = -Math.Abs(feetOnly);

                            textBox.Text = FormatFeetToFeetInchFraction(feetOnly);
                            return;
                        }
                        else if (IsFraction(tokens[0]))
                        {
                            double val = EvaluateMixedFraction("0", tokens[0]);
                            if (isNegative) val = -Math.Abs(val);
                            textBox.Text = FormatFeetToFeetInchFraction(val);
                            return;
                        }
                    }
                    else
                    {
                        // Trường hợp đặc biệt: "8 1/2" → coi là toàn bộ feet
                        if (IsMixedFraction(tokens))
                        {
                            double feetVal = EvaluateMixedFraction(tokens[0], tokens[1]);
                            if (isNegative)
                                feetVal = -Math.Abs(feetVal);

                            textBox.Text = FormatFeetToFeetInchFraction(feetVal);
                            return;
                        }

                        // >=2 block: feet + inch
                        if (double.TryParse(tokens[0], out double feetVal2))
                        {
                            var inchTokens = tokens.Skip(1);
                            string inchStr = string.Join(" ", inchTokens);
                            double inchVal = FormatInchOnly(inchStr);

                            double totalFeet = Math.Abs(feetVal2) + inchVal / 12.0;
                            if (isNegative)
                                totalFeet = -totalFeet;

                            textBox.Text = FormatFeetToFeetInchFraction(totalFeet);
                            return;
                        }
                    }
                }

                if (!hasFeet && hasInch)
                {
                    // Chỉ có inch, không có feet
                    string inchStr = input.Replace("\"", "").Trim();

                    double inchVal = 0;

                    // Tách các số trong chuỗi inchStr (kể cả khi không có space)
                    var numberMatches = Regex.Matches(inchStr, @"-?\d+(\.\d+)?");
                    foreach (Match match in numberMatches)
                    {
                        inchVal += double.Parse(match.Value);
                    }

                    if (isNegative)
                        inchVal = -Math.Abs(inchVal);

                    double feetFromInch = inchVal / 12.0;

                    textBox.Text = FormatFeetToFeetInchFraction(feetFromInch);
                    return;
                }

                if (hasFeet)
                {
                    // Có dấu feet: tách feet và inch
                    int feetIndex = input.IndexOf('\'');
                    feetPart = input.Substring(0, feetIndex).Trim();
                    inchPart = input.Substring(feetIndex + 1).Trim().TrimEnd('"');

                    if (!double.TryParse(feetPart, out double feetVal))
                        return;

                    double inchVal = FormatInchOnly(inchPart);

                    // 👉 Xử lý cộng phần feet dư từ inch
                    int extraFeet = (int)(inchVal / 12);
                    inchVal = inchVal % 12;
                    feetVal += extraFeet;

                    double totalFeet = Math.Abs(feetVal) + inchVal / 12.0;
                    if (isNegative)
                        totalFeet = -totalFeet;

                    string formatted = FormatFeetToFeetInchFraction(totalFeet);
                    textBox.Text = formatted;
                }
            }
            catch
            {
                // Nếu cần: thông báo lỗi hoặc bỏ qua
            }
        }
    }
}
