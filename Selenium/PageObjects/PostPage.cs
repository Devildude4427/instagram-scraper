using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using OpenQA.Selenium;
using Selenium.Utility;

namespace Selenium.PageObjects
{
    public class PostPage
    {
        private readonly WebDriverExtensions _webHelper;
        
        private List<string> _tempLinkList = new List<string>();

        private readonly Queue<KeyValuePair<string, string>> _downloadQueue;

        private readonly string _path;

        public PostPage(IWebDriver driver, string fileSavePath, Queue<KeyValuePair<string, string>> downloadQueue)
        {
            _downloadQueue = downloadQueue;
            _webHelper = new WebDriverExtensions(driver);
            _path = fileSavePath + "/";
        }

        private IWebElement MultiSrcPostChevron => _webHelper.SafeFindElement(".coreSpriteRightChevron");
        
        private IWebElement NextPostPaginationArrow => _webHelper.SafeFindElement(".coreSpriteRightPaginationArrow");
        
        private IWebElement PostTimeStamp => _webHelper.SafeFindElement("time[datetime]");
        
        private IEnumerable<IWebElement> ImageSourceClass => _webHelper.SafeFindElements(".kPFhm img");
        
        private IEnumerable<IWebElement> VideoSourceClass => _webHelper.SafeFindElements(".tWeCl");
        

        public void GetPostData(UriNameDictionary resourceDictionary)
        {
            try
            {
                _webHelper.FindElement(By.CssSelector(".eo2As"), 5);

                if (MultiSrcPostChevron != null)
                {
                    if (VideoSourceClass.Any())
                    {
                        foreach (var webElement in VideoSourceClass)
                        {
                            _tempLinkList.Add(webElement.GetAttribute("src"));
                        }
                    }
                    else if (ImageSourceClass.Any())
                    {
                        foreach (var webElement in ImageSourceClass)
                        {
                            var stringList = webElement.GetAttribute("srcset").Split(',');
                            var index = Array.FindIndex(stringList, row => row.Contains("1080w"));
                            _tempLinkList.Add(stringList[index].Remove(stringList[index].Length - 6));
                        }
                    }

                    MultiSrcPostChevron.Click();
                    GetPostData(resourceDictionary);
                }
                else
                {
                    if (VideoSourceClass.Any())
                    {
                        foreach (var webElement in VideoSourceClass)
                        {
                            _tempLinkList.Add(webElement.GetAttribute("src"));
                        }
                    }
                    else if (ImageSourceClass.Any())
                    {
                        foreach (var webElement in ImageSourceClass)
                        {
                            var stringList = webElement.GetAttribute("srcset").Split(',');
                            var index = Array.FindIndex(stringList, row => row.Contains("1080w"));
                            _tempLinkList.Add(stringList[index].Remove(stringList[index].Length - 6));
                        }
                    }

                    _tempLinkList = _tempLinkList.Distinct().ToList();
                    var timeStamp = RefineTimeStamp();

                    for (var i = 0; i < _tempLinkList.Count; i++)
                    {
                        resourceDictionary.Add(timeStamp + " " + (_tempLinkList.Count - i), _tempLinkList[i]);
                        _downloadQueue.Enqueue(new KeyValuePair<string,string>(timeStamp + " " + (_tempLinkList.Count - i), _tempLinkList[i]));
                        DownloadFile();
                    }

                    if (WebDriverExtensions.IsElementPresent(NextPostPaginationArrow))
                    {
                        NextPostPaginationArrow.Click();
                        _tempLinkList.Clear();
                        GetPostData(resourceDictionary);
                    }
                    else
                    {
                        DownloadFile();
                        Console.WriteLine("Finished");
                        //finish
                    }
                }
            }
            catch (StaleElementReferenceException)
            {
                Console.WriteLine("Stale Element, Retrying");
                GetPostData(resourceDictionary);
            }
        }

        private string RefineTimeStamp()
        {
            var timeStamp = PostTimeStamp.GetAttribute("datetime");
            timeStamp = timeStamp.Substring(0, 10) + " " + timeStamp.Substring(12, 7);
            return timeStamp;
        }

        private void DownloadFile()
        {
            if(!File.Exists(_path)) {Directory.CreateDirectory(_path);}
            var client = new WebClient();
            
            while (!_downloadQueue.Any()) return;
            
            var url = _downloadQueue.Dequeue();

            if (File.Exists(_path + url.Key + ".*")) return;
            if (url.Value.Contains(".mp4"))
            {
                client.DownloadFileAsync(new Uri(url.Value), _path + url.Key + ".mp4");
            }
            else
            {
                client.DownloadFileAsync(new Uri(url.Value), _path + url.Key + ".jpg");
            }
        }
    }
}