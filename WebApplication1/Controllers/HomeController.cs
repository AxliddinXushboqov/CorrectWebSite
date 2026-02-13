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
            var options = new LaunchOptions
            {
                Headless = true,
                Args = new[] {
                    "--no-sandbox",
                    "--disable-setuid-sandbox",
                    "--disable-dev-shm-usage",
                    "--disable-gpu",
                    "--single-process"
    }
            };

            bool isHeroku = Environment.GetEnvironmentVariable("PORT") != null;

            if (!isHeroku)
            {
                // Lokal kompyuteringiz uchun (Yandex manzili qolsin)
                options.ExecutablePath = @"C:\Users\User\AppData\Local\Yandex\YandexBrowser\Application\browser.exe";
            }
            else
            {
                // HEROKUDA: Hech qanday path ko'rsatmaymiz! 
                // Faqat buildpack o'rnatgan o'zgaruvchini tekshiramiz
                var chromeBin = Environment.GetEnvironmentVariable("GOOGLE_CHROME_BIN");
                if (!string.IsNullOrEmpty(chromeBin))
                {
                    options.ExecutablePath = chromeBin;
                }
                // Agar chromeBin ham bo'sh bo'lsa, Puppeteer o'zi default joylardan qidiradi
            }

            try
            {
                using var browser = await Puppeteer.LaunchAsync(options);
                // ... qolgan kodlar
                using var page = await browser.NewPageAsync();

                await page.SetUserAgentAsync("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36...");

                await page.GoToAsync("https://license.gov.uz/registry/591c96fe-de93-4f1d-8d26-c1c5974cade3");

                try
                {
                    await page.WaitForSelectorAsync(".InfoBlock_wrapper--red__jtp2-", new WaitForSelectorOptions { Timeout = 30000 });
                }
                catch
                {
                    // Topilmasa ham davom etaveradi
                }

                try
                {
                    await page.WaitForSelectorAsync("[class*='InfoBlock_wrapper']", new WaitForSelectorOptions { Timeout = 15000 });
                }
                catch
                {
                }

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

                string fullHtml = await page.GetContentAsync();

                fullHtml = fullHtml.Replace("<script", "");
                fullHtml = fullHtml.Replace("<head>", "<head><base href='https://license.gov.uz/'>");

                return Content(fullHtml, "text/html");
            }
            catch (Exception ex)
            {
                return Content($"Xatolik yuz berdi: {ex.Message}");
            }
        }
    }
}
