using Microsoft.AspNetCore.Mvc;
using PuppeteerSharp;
using System.Diagnostics;
using WebApplication1.Models;

namespace WebApplication1.Controllers
{
    public class HomeController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public HomeController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        [Route("registry/591c96fe-de93-4f1d-8d26-c1c5974cade3")]
        [HttpGet]
        public async Task<IActionResult> ShowRegistryPage()
        {
            // 1. Puppeteer variantlarini sozlash
            var options = new LaunchOptions
            {
                Headless = true,
                Args = new[] {
                "--no-sandbox",
                "--disable-setuid-sandbox",
                "--disable-dev-shm-usage",
                "--disable-gpu",
                "--disable-blink-features=AutomationControlled"
    }
            };

            bool isHeroku = Environment.GetEnvironmentVariable("PORT") != null;

            if (!isHeroku)
            {
                options.ExecutablePath = @"C:\Users\User\AppData\Local\Yandex\YandexBrowser\Application\browser.exe";
            }
            else
            {
                await new BrowserFetcher().DownloadAsync();
            }

            try
            {
                using var browser = await Puppeteer.LaunchAsync(options);
                using var page = await browser.NewPageAsync();

                await page.SetUserAgentAsync("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36...");

                await page.GoToAsync("https://license.gov.uz/registry/591c96fe-de93-4f1d-8d26-c1c5974cade3",
                    new NavigationOptions { WaitUntil = new[] { WaitUntilNavigation.Load }, Timeout = 60000 });

                await Task.Delay(5000);

                await page.EvaluateFunctionAsync(@"() => {
                const targetText = 'Муддати тугаган';
                const replacement = 'Фаол';
                const archiveText = 'Архив';
    
                // Sanalar uchun qidiruv va yangi qiymatlar
                const dateOld1 = '20.12.2019';
                const dateNew1 = '20.12.2021';
                const dateOld2 = '20.12.2024';
                const dateNew2 = '20.12.2026';

                const walker = document.createTreeWalker(document.body, NodeFilter.SHOW_TEXT, null, false);
                let node;
                const nodesToProcess = [];
                while(node = walker.nextNode()) {
                    nodesToProcess.push(node);
                }

                nodesToProcess.forEach(textNode => {
                    let text = textNode.nodeValue;

                    // 1. SANALARNI ALMASHTIRISH
                    if (text.includes(dateOld1)) {
                        text = text.replace(new RegExp(dateOld1, 'g'), dateNew1);
                    }
                    if (text.includes(dateOld2)) {
                        text = text.replace(new RegExp(dateOld2, 'g'), dateNew2);
                    }
        
                    // Yangilangan matnni tugunga qaytaramiz
                    textNode.nodeValue = text;

                    // 2. MUDDATI TUGAGAN -> YANGILANDI (VA YASHIL QILISH)
                    if (text.includes(targetText)) {
                        textNode.nodeValue = text.replace(targetText, replacement);
                        let parent = textNode.parentElement;
                        while (parent && parent !== document.body) {
                            const className = parent.className || '';
                            if (className.includes('red') || className.includes('danger')) {
                                parent.style.setProperty('background-color', '#28a745', 'important');
                                parent.style.setProperty('background', '#28a745', 'important');
                                parent.style.setProperty('color', '#ffffff', 'important');
                                parent.style.setProperty('--status-red', '#28a745', 'important');
                                break;
                            }
                            parent = parent.parentElement;
                        }
                    }

                    // 3. АРХИВ -> BO'SHLIQ (VA YO'QOTISH)
                    if (text.includes(archiveText)) {
                        textNode.nodeValue = textNode.nodeValue.replace(archiveText, '');
                        let parent = textNode.parentElement;
                        while (parent && parent !== document.body) {
                            const className = parent.className || '';
                            if (className.includes('danger') || className.includes('red')) {
                                parent.style.display = 'none'; 
                                break;
                            }
                            parent = parent.parentElement;
                        }
                    }
                });

                window.stop();
            }");

                // 5. Tozalangan HTMLni olamiz
                string fullHtml = await page.GetContentAsync();

                // 6. Xavfsizlik va Stillar
                fullHtml = fullHtml.Replace("<script", "");
                fullHtml = fullHtml.Replace("<head>", "<head><base href='https://license.gov.uz/'>");

                // 7. NATIJANI TO'G'RIDAN-TO'G'RI BRAUZERGA CHIQARAMIZ
                // View() emas, Content() ishlatamiz, shunda HTML xuddi o'zidek ochiladi
                return Content(fullHtml, "text/html");
            }
            catch (Exception ex)
            {
                return Content($"Xatolik yuz berdi: {ex.Message}");
            }
        }
    }
}
