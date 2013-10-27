using System;
using System.Text;

namespace Glimpse.Helpers
{
    public class StringHelper
    {
        public static String GenerateRandomString(Int16 size = 16)
        {
            StringBuilder builder = new StringBuilder();
            Random random = new Random();
            char ch;
            for (Int16 i = 0; i < size; i++)
            {
                ch = Convert.ToChar(Convert.ToInt32(Math.Floor(26 * random.NextDouble() + 65)));
                builder.Append(ch);
            }
            return builder.ToString();
        }
    }
}