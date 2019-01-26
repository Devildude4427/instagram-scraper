using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks.Dataflow;
using Instagram_Scraper.Utility;
using OpenQA.Selenium;

namespace Instagram_Scraper.PageObjects
{
    public class PostPage
    {
        private readonly IWebDriver _driver;
        
        private readonly WebDriverExtensions _webHelper;

        private List<string> _tempLinkList = new List<string>();
        
        private readonly ITargetBlock<KeyValuePair<string, string>> _targetMedia;

        private readonly ITargetBlock<KeyValuePair<string, List<KeyValuePair<string, string>>>> _targetText;

        public PostPage(IWebDriver driver, ITargetBlock<KeyValuePair<string, string>> targetMediaMedia)
        {
            _webHelper = new WebDriverExtensions(driver);
            _targetMedia = targetMediaMedia;
            _driver = driver;
        }

        public PostPage(IWebDriver driver, ITargetBlock<KeyValuePair<string, string>> targetMediaMedia,
            ITargetBlock<KeyValuePair<string, List<KeyValuePair<string, string>>>> targetText)
        {
            _webHelper = new WebDriverExtensions(driver);
            _targetMedia = targetMediaMedia;
            _driver = driver;
            _targetText = targetText;
        }

        private IWebElement MultiSrcPostChevron => _webHelper.SafeFindElement(".coreSpriteRightChevron");

        private IWebElement NextPostPaginationArrow => _webHelper.SafeFindElement(".coreSpriteRightPaginationArrow");

        private IEnumerable<IWebElement> ImageSourceClass => _webHelper.SafeFindElements(".kPFhm img");

        private IEnumerable<IWebElement> VideoSourceClass => _webHelper.SafeFindElements(".tWeCl");
        
        private IWebElement ViewAllCommentsClass => _webHelper.SafeFindElement(".lnrre");

        //TODO Issues with posts. Occasionally, multi-src posts are skipped over entirely
        public void GetPostData()
        {
            try
            {
                _webHelper.WaitForElement(By.CssSelector(".eo2As"), 2000);

                if (MultiSrcPostChevron != null)
                {
                    if (VideoSourceClass.Any())
                        foreach (var webElement in VideoSourceClass)
                            _tempLinkList.Add(webElement.GetAttribute("src"));
                    else if (ImageSourceClass.Any())
                        foreach (var webElement in ImageSourceClass)
                        {
                            var stringList = webElement.GetAttribute("srcset").Split(',');
                            var index = Array.FindIndex(stringList, row => row.Contains("1080w"));
                            _tempLinkList.Add(stringList[index].Remove(stringList[index].Length - 6));
                        }

                    MultiSrcPostChevron.Click();
                    GetPostData();
                }
                else
                {
                    if (VideoSourceClass.Any())
                        foreach (var webElement in VideoSourceClass)
                            _tempLinkList.Add(webElement.GetAttribute("src"));
                    else if (ImageSourceClass.Any())
                        foreach (var webElement in ImageSourceClass)
                        {
                            var stringList = webElement.GetAttribute("srcset").Split(',');
                            var index = Array.FindIndex(stringList, row => row.Contains("1080w"));
                            _tempLinkList.Add(stringList[index].Remove(stringList[index].Length - 6));
                        }

                    _tempLinkList = _tempLinkList.Distinct().ToList();
                    var timeStamp = _webHelper.RefineTimeStamp();

                    for (var i = 0; i < _tempLinkList.Count; i++)
                        _targetMedia.Post(new KeyValuePair<string, string>(timeStamp + " " + (_tempLinkList.Count - i),
                            _tempLinkList[i]));

                    if (NextPostPaginationArrow != null)
                    {
                        NextPostPaginationArrow.Click();
                        _tempLinkList.Clear();
                        new PostPage(_driver, _targetMedia).GetPostData();
                    }
                    else
                    {
                        _targetMedia.Complete();
                        Console.WriteLine("Finished capture of post data");
                    }
                }
            }
            catch (StaleElementReferenceException)
            {
                Console.WriteLine("Stale Element, Retrying");
                GetPostData();
            }
        }

        public void GetPostDataWithComments()
        {
            try
            {
                _webHelper.WaitForElement(By.CssSelector(".eo2As"), 2000);

                if (MultiSrcPostChevron != null)
                {
                    if (VideoSourceClass.Any())
                        foreach (var webElement in VideoSourceClass)
                            _tempLinkList.Add(webElement.GetAttribute("src"));
                    else if (ImageSourceClass.Any())
                        foreach (var webElement in ImageSourceClass)
                        {
                            var stringList = webElement.GetAttribute("srcset").Split(',');
                            var index = Array.FindIndex(stringList, row => row.Contains("1080w"));
                            _tempLinkList.Add(stringList[index].Remove(stringList[index].Length - 6));
                        }

                    MultiSrcPostChevron.Click();
                    GetPostDataWithComments();
                }
                else
                {
                    if (VideoSourceClass.Any())
                        foreach (var webElement in VideoSourceClass)
                            _tempLinkList.Add(webElement.GetAttribute("src"));
                    else if (ImageSourceClass.Any())
                        foreach (var webElement in ImageSourceClass)
                        {
                            var stringList = webElement.GetAttribute("srcset").Split(',');
                            var index = Array.FindIndex(stringList, row => row.Contains("1080w"));
                            _tempLinkList.Add(stringList[index].Remove(stringList[index].Length - 6));
                        }

                    _tempLinkList = _tempLinkList.Distinct().ToList();
                    var timeStamp = _webHelper.RefineTimeStamp();

                    for (var i = 0; i < _tempLinkList.Count; i++)
                        _targetMedia.Post(new KeyValuePair<string, string>(timeStamp + " " + (_tempLinkList.Count - i),
                            _tempLinkList[i]));

                    ViewAllCommentsClass?.Click();
                    var commentUserClass = _webHelper.SafeFindElements(".FPmhX").ToList();
                    var commentTextClass = _webHelper.SafeFindElements("div.C4VMK span").ToList();

                    //First element is a header for the post, which is unrelated to the comments section
                    commentUserClass.RemoveAt(0);

                    var commentUsernameList =
                        commentUserClass.Select(username => username.GetAttribute("title")).ToList();
                    var commentTextList = commentTextClass.Select(text => text.Text).ToList();

                    var x = commentUsernameList
                        .Select((t, i) => new KeyValuePair<string, string>(t, commentTextList[i])).ToList();

                    _targetText.Post(new KeyValuePair<string, List<KeyValuePair<string, string>>>(timeStamp, x));


                    if (NextPostPaginationArrow != null)
                    {
                        NextPostPaginationArrow.Click();
                        _tempLinkList.Clear();
                        new PostPage(_driver, _targetMedia, _targetText).GetPostDataWithComments();
                    }
                    else
                    {
                        _targetMedia.Complete();
                        _targetText.Complete();
                        Console.WriteLine("Finished");
                    }
                }
            }
            catch (StaleElementReferenceException)
            {
                Console.WriteLine("Stale Element, Retrying");
                GetPostData();
            }
        }
    }
}