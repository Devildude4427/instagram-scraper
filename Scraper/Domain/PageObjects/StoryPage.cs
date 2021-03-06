using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks.Dataflow;
using Instagram_Scraper.Utility;
using NLog;
using OpenQA.Selenium;

namespace Instagram_Scraper.Domain.PageObjects
{
    public class StoryPage
    {
        private static readonly Logger Logger = LogManager.GetLogger("Story Page");
        
        private readonly WebDriverExtensions _webHelper;
        
        private readonly List<string> _tempLinkList = new List<string>();
        
        private readonly ITargetBlock<KeyValuePair<string, string>> _targetStory;

        public StoryPage(IWebDriver driver, ITargetBlock<KeyValuePair<string, string>> targetStory)
        {
            _webHelper = new WebDriverExtensions(driver);
            _targetStory = targetStory; 
        }

        private IWebElement StoryChevronClass => _webHelper.SafeFindElement(".ow3u_");

        private IEnumerable<IWebElement> StoryVideoSrcClass => _webHelper.SafeFindElements(".OFkrO source");

        private IEnumerable<IWebElement> StoryImageSrcClass => _webHelper.SafeFindElements(".y-yJ5");

        private IEnumerable<IWebElement> StoryPageNavigationClass => _webHelper.SafeFindElements("._7zQEa");

        public void SaveStoryContent()
        {
            try
            {
                _webHelper.WaitForElement(By.CssSelector("._7zQEa"), 2000);

                if (StoryVideoSrcClass.Any())
                    _tempLinkList.Add(StoryVideoSrcClass.First().GetAttribute("src"));
                else if (StoryImageSrcClass.Any())
                    foreach (var webElement in StoryImageSrcClass)
                    {
                        var stringList = webElement.GetAttribute("srcset").Split(',');
                        var index = Array.FindIndex(stringList, row => row.Contains("1080w"));
                        _tempLinkList.Add(stringList[index].Remove(stringList[index].Length - 6));
                    }

                if (_tempLinkList.Count < StoryPageNavigationClass.Count())
                {
                    StoryChevronClass.Click();
                    SaveStoryContent();
                }
                else
                {
                    var currentDateTime = DateTime.Now.ToString("yyyy-MM-dd");
                    for (var i = 0; i < _tempLinkList.Count; i++)
                    {
                        _targetStory.Post(new KeyValuePair<string, string>(currentDateTime + " story " +
                                                                           (_tempLinkList.Count - i),
                            _tempLinkList[i]));
                    }

                    Logger.Info("Story Capture Complete");
                    _targetStory.Complete();
                    StoryChevronClass.Click();
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "There's a good chance your password is wrong");
            }
            
        }
    }
}