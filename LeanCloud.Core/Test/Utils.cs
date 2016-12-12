using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LeanCloud.Core;
using LeanCloud;
using LeanCloud.Core.Internal;
using NUnit.Framework;
using Moq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Configuration;

namespace ParseTest
{
    public class Utils
    {
        public static string RandomString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789abcdefghijklmnopqrstuvwxyz";
            var random = new Random();
            return new string(Enumerable.Repeat(chars, length)
              .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        public static string RandomUsername()
        {
            return RandomString(6);
        }
        public static Task<AVUser> SignUp()
        {
            var p = Utils.RandomString(10);
            return SignUp(p);
        }
        public static Task<AVUser> SignUp(string pwd)
        {
            var u = Utils.RandomUsername();
           
            AVUser user = new AVUser()
            {
                Username = u,
                Password = pwd,
                MobilePhoneNumber = GetRandomTel()
            };

            return user.SignUpAsync().ContinueWith(t =>
            {
                return user;
            });
        }

        private static string[] telStarts = "134,135,136,137,138,139,150,151,152,157,158,159,130,131,132,155,156,133,153,180,181,182,183,185,186,176,187,188,189,177,178".Split(',');


        /// <summary>
        /// 随机生成电话号码
        /// </summary>
        /// <returns></returns>
        public static string GetRandomTel()
        {
            var ran = new Random();
            int n = ran.Next(10, 1000);
            int index = ran.Next(0, telStarts.Length - 1);
            string first = telStarts[index];
            string second = (ran.Next(100, 888) + 10000).ToString().Substring(1);
            string thrid = (ran.Next(1, 9100) + 10000).ToString().Substring(1);
            return first + second + thrid;
        }
    }
}
