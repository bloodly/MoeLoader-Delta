﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Reflection;
using System.Windows.Forms;

namespace MoeLoaderDelta
{
    /// <summary>
    /// 管理站点定义
    /// Last 20190117
    /// </summary>
    public class SiteManager
    {
        private static List<ImageSite> sites = new List<ImageSite>();
        private static SiteManager instance;
        private static string runPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        private static string sitePacksPath = $"{runPath}\\SitePacks\\";

        private SiteManager()
        {

            string[] dlls = Directory.GetFiles(sitePacksPath, "SitePack*.dll", SearchOption.AllDirectories);

            #region 保证有基本站点包路径
            if (dlls.Length < 1)
            {
                List<string> dlll = new List<string>();
                string basisdll = sitePacksPath + "SitePack.dll";

                if (File.Exists(basisdll))
                {
                    dlll.Add(basisdll);
                    dlls = dlll.ToArray();
                }
            }
            #endregion

            foreach (string dll in dlls)
            {
                try
                {
                    byte[] assemblyBuffer = File.ReadAllBytes(dll);
                    Type type = Assembly.Load(assemblyBuffer).GetType("SitePack.SiteProvider", true, false);
                    MethodInfo methodInfo = type.GetMethod("SiteList");
                    sites.AddRange(methodInfo.Invoke(Activator.CreateInstance(type), new object[] { Mainproxy }) as List<ImageSite>);
                }
                catch (Exception ex)
                {
                    echoErrLog("站点载入过程", ex);
                }
            }
        }

        /// <summary>
        /// 站点定义管理者
        /// </summary>
        public static SiteManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new SiteManager();
                }
                return instance;
            }
        }

        /// <summary>
        /// 站点集合
        /// </summary>
        public List<ImageSite> Sites => sites;

        /// <summary>
        /// 主窗口代理传递
        /// </summary>
        public static IWebProxy Mainproxy { get; set; }

        /// <summary>
        /// 站点登录处理 通过IE
        /// </summary>
        /// <param name="imageSite">站点</param>
        /// <param name="cookie">站点内部cookie, 将返回登录后的cookie, 登录失败为string.Empty</param>
        /// <param name="LoggedFlags">登录成功页面验证字符, 多个字符用|分隔; 无需验证请置null</param>
        /// <param name="Sweb">站点内部SessionClient</param>
        /// <param name="shc">站点内部SessionHeadersCollection</param>
        /// <param name="PageString">返回验证登录时的页面HTML</param>
        /// <returns></returns>
        public static bool LoginSite(ImageSite imageSite, ref string cookie, string LoggedFlags, ref SessionClient Sweb, ref SessionHeadersCollection shc, ref string pageString)
        {
            string tmp_cookie = CookiesHelper.GetIECookies(imageSite.SiteUrl);
            bool result = !string.IsNullOrWhiteSpace(tmp_cookie) && tmp_cookie.Length > 3;

            if (result)
            {
                shc.Set("Cookie", tmp_cookie);
                pageString = Sweb.Get(imageSite.SiteUrl, Mainproxy, shc);
                result = !string.IsNullOrWhiteSpace(pageString);

                if (result && LoggedFlags != null)
                {
                    string[] LFlagsArray = LoggedFlags.Split('|');
                    foreach (string Flag in LFlagsArray)
                    {
                        result &= pageString.Contains(Flag);
                    }
                }
            }
            cookie = result ? tmp_cookie : cookie;
            return result;
        }

        /// <summary>
        /// 站点登录处理 通过IE
        /// </summary>
        /// <param name="imageSite">站点</param>
        /// <param name="cookie">站点内部cookie, 将返回登录后的cookie, 登录失败为string.Empty</param>
        /// <param name="LoggedFlags">登录成功页面验证字符, 多个字符用|分隔; 无需验证请置null</param>
        /// <param name="Sweb">站点内部SessionClient</param>
        /// <param name="shc">站点内部SessionHeadersCollection</param>
        /// <returns></returns>
        public static bool LoginSite(ImageSite imageSite, ref string cookie, string LoggedFlags, ref SessionClient Sweb, ref SessionHeadersCollection shc)
        {
            string NullPageString = string.Empty;
            return LoginSite( imageSite, ref  cookie,  LoggedFlags, ref  Sweb, ref  shc, ref NullPageString);
        }

        /// <summary>
        /// 提供站点错误的输出
        /// </summary>
        /// <param name="SiteShortName">站点短名</param>
        /// <param name="ex">错误信息</param>
        /// <param name="extra_info">附加错误信息</param>
        public static void echoErrLog(string SiteShortName, Exception ex, string extra_info)
        {
            bool exisnull = ex == null;
            string wstr = "[异常站点]: " + SiteShortName + "\r\n";
            wstr += "[异常时间]: " + DateTime.Now.ToString() + "\r\n";
            wstr += "[异常信息]: " + extra_info + (exisnull ? "\r\n" : string.Empty);
            if (!exisnull)
            {
                wstr += (string.IsNullOrWhiteSpace(extra_info) ? string.Empty : " | ") + ex.Message + "\r\n";
                wstr += "[异常对象]: " + ex.Source + "\r\n";
                wstr += "[调用堆栈]: " + ex.StackTrace.Trim() + "\r\n";
                wstr += "[触发方法]: " + ex.TargetSite + "\r\n";
            }
            File.AppendAllText(sitePacksPath + "site_error.log", wstr + "\r\n");
            MessageBox.Show((string.IsNullOrWhiteSpace(extra_info) ? ex.Message : extra_info), $"{SiteShortName} 错误", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        /// <summary>
        /// 提供站点错误的输出
        /// </summary>
        /// <param name="SiteShortName">站点短名</param>
        /// <param name="ex">错误信息</param>
        public static void echoErrLog(string SiteShortName, Exception ex)
        {
            echoErrLog(SiteShortName, ex, null);
        }
        /// <summary>
        /// 提供站点错误的输出
        /// </summary>
        /// <param name="SiteShortName">站点短名</param>
        /// <param name="extra_info">附加错误信息</param>
        public static void echoErrLog(string SiteShortName, string extra_info)
        {
            echoErrLog(SiteShortName, null, extra_info);
        }
    }
}
