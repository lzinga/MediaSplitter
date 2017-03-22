using MediaSplitter.Common;
using MediaSplitter.Services;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace MediaSplitter
{
    public class Arguments
    {

        public string Media { get; set; }

        public List<string> Extensions { get; set; } = new List<string>();

        /// <summary>
        /// If the program should run in debug mode.
        /// </summary>
        public bool Debug { get; set; }

        /// <summary>
        /// The minimum detected black duration (in seconds)
        /// </summary>
        public double BlackDuration { get; set; } = 0.2;

        /// <summary>
        /// Threshold for considering a picture as "Black" (in percent).
        /// </summary>
        public double BlackThreshold { get; set; } = 0.98;

        /// <summary>
        /// Threshold for considering a pixel "black" (in luminance)
        /// </summary>
        public double BlackPixelLuminance { get; set; } = 0.15;

        /// <summary>
        /// If specified will ignore any black frame checks and just cut at this point.
        /// </summary>
        public TimeSpan CutTime { get; set; }

        /// <summary>
        /// The start range to look for black frames.
        /// </summary>
        public TimeSpan StartRange { get; set; }

        /// <summary>
        /// The end range to look for black scenes.
        /// </summary>
        public TimeSpan EndRange { get; set; }


        public Arguments(string[] args)
        {
            if (args.Length >= 1)
            {
                for (int i = 0; i < args.Length; i++)
                {
                    string full = args[i];

                    if (full.Contains("="))
                    {
                        string key = full.Split('=')[0].Replace("/", "").Replace("\\", "").Trim();
                        string value = full.Split('=')[1].Trim();

                        PropertyInfo info = typeof(Arguments).GetProperty(key, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);

                        if (info.PropertyType == typeof(TimeSpan))
                        {
                            TimeSpan span;
                            if(TimeSpan.TryParse(value, out span))
                            {
                                info.SetValue(this, span);
                            }
                        }
                        else if (value.Contains(",") && info.PropertyType.IsGenericType && info.PropertyType.GetGenericTypeDefinition() == typeof(List<>))
                        {
                            Type innerType = info.PropertyType.GetGenericArguments()[0];
                            IList list = info.GetValue(this) as IList;
                            foreach (string basic in value.Split(','))
                            {
                                object item = Convert.ChangeType(basic, innerType);
                                list.Add(item);
                            }
                        }
                        else
                        {
                            info.SetValue(this, value);
                        }
                    }
                    else
                    {
                        string key = full.Replace("/", "").Replace("\\", "").Trim();

                        PropertyInfo info = typeof(Arguments).GetProperty(key, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
                        info.SetValue(this, true);
                    }

                }
            }
        }

        public IEnumerable<string> Get()
        {
            foreach (PropertyInfo info in typeof(Arguments).GetProperties())
            {
                if(!info.PropertyType.IsGenericType)
                {
                    yield return $"{info.Name} = \"{info.GetValue(this)}\"";
                }
                else
                {
                    var list = info.GetValue(this) as IList;

                    // Cannot use string.Join with an IList with no specified type. Which is why I use a foreach here.
                    StringBuilder builder = new StringBuilder();
                    foreach (var i in list)
                    {
                        builder.Append($"{i},");
                    }

                    yield return $"{info.Name} = \"{builder.ToString().TrimEnd(',')}\"";
                }
            }
        }

    }
}
