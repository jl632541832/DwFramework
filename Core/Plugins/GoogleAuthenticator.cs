﻿using System;
using System.Security.Cryptography;
using System.Text;

using DwFramework.Core.Extensions;

namespace DwFramework.Core.Plugins
{
    public static class GoogleAuthenticator
    {
        /// <summary>
        /// 获取手机端秘钥
        /// </summary>
        /// <returns></returns>
        public static string GetMobilePhoneKey(string key)
        {
            var baseCode = Encoding.UTF8.GetBytes(key).ToBase32String();
            return baseCode.TrimEnd('=');
        }

        /// <summary>
        /// 生成认证码
        /// </summary>
        /// <returns>返回验证码</returns>
        public static string GenerateCode(string key)
        {
            var counter = (long)(DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds / 30;
            return GenerateHashedCode(key, counter);
        }

        /// <summary>
        /// 验证
        /// </summary>
        /// <param name="key"></param>
        /// <param name="code"></param>
        /// <returns></returns>
        public static bool VerifyCode(string key, string code)
        {
            return code == GenerateCode(key);
        }

        /// <summary>
        /// 按照次数生成哈希编码
        /// </summary>
        /// <param name="secret">秘钥</param>
        /// <param name="iterationNumber">迭代次数</param>
        /// <param name="digits">生成位数</param>
        /// <returns>返回验证码</returns>
        private static string GenerateHashedCode(string secret, long iterationNumber, int digits = 6)
        {
            byte[] counter = BitConverter.GetBytes(iterationNumber);

            if (BitConverter.IsLittleEndian)
                Array.Reverse(counter);

            byte[] key = Encoding.ASCII.GetBytes(secret);

            HMACSHA1 hmac = new HMACSHA1(key, true);

            byte[] hash = hmac.ComputeHash(counter);

            int offset = hash[hash.Length - 1] & 0xf;

            int binary =
                ((hash[offset] & 0x7f) << 24)
                | ((hash[offset + 1] & 0xff) << 16)
                | ((hash[offset + 2] & 0xff) << 8)
                | (hash[offset + 3] & 0xff);

            int password = binary % (int)Math.Pow(10, digits); // 6 digits

            return password.ToString(new string('0', digits));
        }
    }
}
