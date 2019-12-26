using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace MP3_MusicPlayer
{
    public class NameConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string filePath = value as string;
            string res = filePath.Substring(filePath.LastIndexOf("\\")+1);

            try
            {
                var info = TagLib.File.Create(filePath);

                if (info.Tag.Title != null)
                    res = info.Tag.Title;
                else //remove extension name
                    res = res.Substring(0, res.LastIndexOf("."));
            }
            catch (Exception)
            {

            }

            return res;                
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
