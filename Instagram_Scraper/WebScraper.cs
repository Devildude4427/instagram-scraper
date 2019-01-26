using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks.Dataflow;
using Instagram_Scraper.PageObjects;
using Instagram_Scraper.Utility;
using OpenQA.Selenium.Firefox;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;

namespace Instagram_Scraper
{
    public static class WebScraper
    {
        private static IWebDriver _driver;

        public static async void SetUp(string targetAccount, bool scrapeStory, string username, string password,
                                       string folderSavePath, bool headless, bool firefoxProfile, bool scrapeComments)
        {
            if (firefoxProfile)
            {
                var optionsFireFox = new FirefoxOptions();
                optionsFireFox.SetPreference("permissions.default.image", 2);
                optionsFireFox.SetPreference("dom.ipc.plugins.enabled.libflashplayer.so", false);
                if (headless) { optionsFireFox.AddArgument("--headless"); }
                _driver = new FirefoxDriver(optionsFireFox);
            }
            else
            {
                var optionsChrome = new ChromeOptions();
                optionsChrome.AddUserProfilePreference("profile.default_content_setting_values.images", 2);
                optionsChrome.AddArgument("--disable-popup-blocking");
                optionsChrome.AddArgument("--window-size=1920,1080");
            
                if (headless) { optionsChrome.AddArgument("headless"); }
                _driver = new ChromeDriver(optionsChrome);
            }


            string savePath;
            var homePath = (Environment.OSVersion.Platform == PlatformID.Unix || 
                               Environment.OSVersion.Platform == PlatformID.MacOSX)
                ? Environment.GetEnvironmentVariable("HOME")
                : Environment.ExpandEnvironmentVariables("%HOMEDRIVE%%HOMEPATH%");
            if (folderSavePath.Equals(""))
            {
                savePath = homePath + "/Pictures/" + targetAccount + "/";
            }
            else
            {
                var folderSavePathSections = folderSavePath.Split("/");
                var maxIndex = folderSavePathSections.Length - 1;
                if (folderSavePathSections[maxIndex].Contains(targetAccount) ||
                    folderSavePathSections[maxIndex].Equals(targetAccount, StringComparison.InvariantCultureIgnoreCase))
                {
                    savePath = folderSavePath + "/";
                }
                else
                {
                    savePath = folderSavePath + "/" + targetAccount + "/";
                } 
            }
            
            
            var bufferMedia = new BufferBlock<KeyValuePair<string, string>>();
            var backgroundThreadMedia =
                new Thread(() => DownloadManager.ConsumeFilesAsync(savePath, bufferMedia)) {IsBackground = true};
            backgroundThreadMedia.Start();
            
            var bufferText = new BufferBlock<KeyValuePair<string, List<KeyValuePair<string, string>>>>();

            if (scrapeComments)
            {
                var backgroundThreadText =
                    new Thread(() => DownloadManager.ConsumeTextAsync(savePath, bufferText)) {IsBackground = true};
                backgroundThreadText.Start();
            }
            
            
            ExecuteScraper(targetAccount, bufferMedia, bufferText, scrapeStory, username, password, scrapeComments);

            await bufferMedia.Completion;
            if (scrapeComments) await bufferText.Completion;
            
            _driver.Quit();
        }

        private static void ExecuteScraper(string targetAccount, ITargetBlock<KeyValuePair<string, string>> targetMedia, ITargetBlock<KeyValuePair<string, List<KeyValuePair<string, string>>>> targetText,
                                           bool scrapeStory, string username, string password, bool scrapeComments)
        {
            var watch = System.Diagnostics.Stopwatch.StartNew();
            if (!password.Equals(""))
            {
                var loginPage = new LoginPage(_driver);
                loginPage.Login(username, password);
            }
            watch.Stop();
            var loginTime = watch.ElapsedMilliseconds;
            Console.WriteLine("Time to login: " + loginTime/1000.00 + " seconds");
            
            watch.Restart();
            var profilePage = new ProfilePage(_driver);
            profilePage.GoToProfile(targetAccount);
            profilePage.GetProfilePicture(targetMedia);

            if (scrapeStory)
            {
                var storyPage = profilePage.EnterStory(targetMedia);
                storyPage?.SaveStoryContent();
            }

            var postPage = scrapeComments ? profilePage.EnterPosts(targetMedia, targetText) : profilePage.EnterPosts(targetMedia);
            
            watch.Stop();
            var enterPostTime = watch.ElapsedMilliseconds;
            Console.WriteLine("Time to enter post: " + enterPostTime/1000.00 + " seconds");
            
            
            watch.Restart();
            
            if (scrapeComments) postPage.GetPostDataWithComments();
            else postPage.GetPostData();
            
            watch.Stop();
            var getPostPicturesTime = watch.ElapsedMilliseconds;
            Console.WriteLine("Time to get all post pictures: " + getPostPicturesTime/1000.00 + " seconds");
            
            
            Console.WriteLine("Total Program Time: " + (loginTime + enterPostTime + getPostPicturesTime)/1000.00
                                                     + " seconds");
        }
    }
}