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
            "--single-process", // Heroku RAM limitidan oshmaslik uchun
            "--no-zygote"
        }
            };

            // Heroku muhitida GOOGLE_CHROME_BIN o'zgaruvchisi avtomatik o'rnatiladi
            string chromeBin = Environment.GetEnvironmentVariable("GOOGLE_CHROME_BIN");
            bool isHeroku = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("PORT"));

            if (!isHeroku)
            {
                // Lokal kompyuteringiz uchun
                options.ExecutablePath = @"C:\Users\User\AppData\Local\Yandex\YandexBrowser\Application\browser.exe";
            }
            else
            {
                // HEROKU UCHUN: Agar o'zgaruvchi bo'lsa uni ishlatamiz, 
                // bo'lmasa Puppeteer o'zi default joylardan qidiradi
                if (!string.IsNullOrEmpty(chromeBin))
                {
                    options.ExecutablePath = chromeBin;
                }
                else
                {
                    // Buildpack ba'zan mana bu yerga o'rnatadi:
                    options.ExecutablePath = "/app/.apt/usr/bin/google-chrome";
                }
            }

            try
            {
                // ... qolgan barcha kodlar (GoToAsync, EvaluateFunction, etc.)
                using var browser = await Puppeteer.LaunchAsync(options);
                using var page = await browser.NewPageAsync();

                // Brauzer "tirikligini" bildirish uchun UserAgent
                await page.SetUserAgentAsync("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/121.0.0.0 Safari/537.36");

                // 1. Sahifani ochish (Timeoutni 2 daqiqa qildik)
                await page.GoToAsync("https://license.gov.uz/registry/591c96fe-de93-4f1d-8d26-c1c5974cade3",
                    new NavigationOptions { WaitUntil = new[] { WaitUntilNavigation.DOMContentLoaded }, Timeout = 120000 });

                // 2. Modalka (oynacha) yuklanishini kutish (Timeout 30s)
                try
                {
                    await page.WaitForSelectorAsync("[class*='InfoBlock_wrapper']", new WaitForSelectorOptions { Timeout = 30000 });
                }
                catch { }

                // 3. Statuslarni va sanalarni o'zgartirish
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

                // 4. HTMLni olish va Regex bilan skriptlarni butunlay tozalash
                string fullHtml = await page.GetContentAsync();

                // Bu kod tepada chiqib qolgan yozuvlarni butunlay yo'qotadi
                fullHtml = System.Text.RegularExpressions.Regex.Replace(fullHtml, @"<script\b[^<]*(?:(?!<\/script>)<[^<]*)*<\/script>", "", System.Text.RegularExpressions.RegexOptions.IgnoreCase);

                fullHtml = fullHtml.Replace("<head>", "<head><base href='https://license.gov.uz/'>");

                return Content(fullHtml, "text/html");
            }
            catch (Exception ex)
            {
                return Content($"Xatolik: {ex.Message}");
            }
        }
    }
}
