// See https://aka.ms/new-console-template for more information
Console.WriteLine("Hello, World!");

using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

string outputDir = Path.Combine(Directory.GetCurrentDirectory(), "..", "EventManagementSystem", "wwwroot", "images", "hosts");
Directory.CreateDirectory(outputDir);

GenerateHostImage("Host", Path.Combine(outputDir, "default.jpg"));
GenerateHostImage("John Smith", Path.Combine(outputDir, "john-smith.jpg"));
GenerateHostImage("Sarah Johnson", Path.Combine(outputDir, "sarah-johnson.jpg"));

Console.WriteLine("Host images generated successfully!");

void GenerateHostImage(string text, string outputPath)
{
    int width = 200;
    int height = 200;
    
    using (Bitmap bitmap = new Bitmap(width, height))
    {
        using (Graphics graphics = Graphics.FromImage(bitmap))
        {
            // Set high quality
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
            graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
            
            // Fill background
            graphics.Clear(Color.FromArgb(224, 224, 224)); // Light gray background
            
            // Get initials
            string[] nameParts = text.Split(' ');
            string initials = nameParts.Length > 1 
                ? $"{nameParts[0][0]}{nameParts[1][0]}"
                : text.Length > 0 ? text[0].ToString() : "?";
            
            // Draw circle background
            using (Brush circleBrush = new SolidBrush(Color.FromArgb(70, 130, 180))) // Steel blue
            {
                graphics.FillEllipse(circleBrush, 50, 50, 100, 100);
            }
            
            // Draw initials
            using (Font font = new Font("Arial", 36, FontStyle.Bold))
            {
                SizeF textSize = graphics.MeasureString(initials, font);
                
                // Draw text in center of circle
                using (Brush textBrush = new SolidBrush(Color.White))
                {
                    graphics.DrawString(
                        initials, 
                        font, 
                        textBrush, 
                        new PointF((width - textSize.Width) / 2, (height - textSize.Height) / 2)
                    );
                }
            }
            
            // Draw full name at bottom
            using (Font nameFont = new Font("Arial", 14, FontStyle.Bold))
            {
                SizeF nameSize = graphics.MeasureString(text, nameFont);
                
                // Draw shadow for better visibility
                using (Brush shadowBrush = new SolidBrush(Color.FromArgb(100, 0, 0, 0)))
                {
                    graphics.DrawString(
                        text,
                        nameFont,
                        shadowBrush,
                        new PointF((width - nameSize.Width) / 2 + 1, 160 + 1)
                    );
                }
                
                // Draw name text
                using (Brush nameBrush = new SolidBrush(Color.FromArgb(44, 62, 80)))
                {
                    graphics.DrawString(
                        text,
                        nameFont,
                        nameBrush,
                        new PointF((width - nameSize.Width) / 2, 160)
                    );
                }
            }
        }
        
        bitmap.Save(outputPath, ImageFormat.Jpeg);
        Console.WriteLine($"Generated image for '{text}' at {outputPath}");
    }
}
