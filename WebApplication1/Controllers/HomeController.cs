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

        // Bu atribut orqali biz xohlagan URL manzilni metodga ulaymiz
        [Route("registry/591c96fe-de93-4f1d-8d26-c1c5974cade3")]
        [HttpGet] // Foydalanuvchi brauzerda manzilni yozganda GET so'rovi ketadi
        public async Task<IActionResult> ShowRegistryPage()
        {
            // 1. Puppeteer variantlarini sozlash
            var options = new LaunchOptions
            {
                Headless = true,
                Args = new[] { "--no-sandbox", "--disable-setuid-sandbox", "--disable-blink-features=AutomationControlled" }
            };

            // Heroku va lokal uchun executable pathni tekshirish (boyagi kod)
            if (Environment.GetEnvironmentVariable("PORT") == null)
            {
                options.ExecutablePath = @"C:\Users\User\AppData\Local\Yandex\YandexBrowser\Application\browser.exe";
            }

            try
            {
                using var browser = await Puppeteer.LaunchAsync(options);
                using var page = await browser.NewPageAsync();

                // Brauzer identifikatorini o'rnatish
                await page.SetUserAgentAsync("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36...");

                // 2. Asl saytni ochamiz
                await page.GoToAsync("https://license.gov.uz/registry/591c96fe-de93-4f1d-8d26-c1c5974cade3",
                    new NavigationOptions { WaitUntil = new[] { WaitUntilNavigation.Load }, Timeout = 60000 });

                // 3. Oynacha yuklanishini kutamiz
                await Task.Delay(15000);

                // 4. Barcha matnlarni almashtirish (Sana, Status, Arxiv - boyagi JS kodingiz)
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

        [Route("registry/591c96fe-de93-4f1d-8d26-c1c5974cade3")]
        [HttpPost]
        public async Task<JsonResult> GetCleanData()
        {
            var options = new LaunchOptions
            {
                Headless = true, // Herokuda faqat true ishlaydi
                ExecutablePath = @"C:\Users\User\AppData\Local\Yandex\YandexBrowser\Application\browser.exe",
                Args = new[] {
            "--no-sandbox",
            "--disable-setuid-sandbox",
            "--disable-blink-features=AutomationControlled", // Robotlikni yashirish
            "--use-fake-ui-for-media-stream",
            "--disable-infobars"
        }
            };

            try
            {
                using var browser = await Puppeteer.LaunchAsync(options);
                using var page = await browser.NewPageAsync();

                // 1. Robot ekanligimizni bildiruvchi 'navigator.webdriver' belgisini o'chiramiz
                await page.EvaluateExpressionOnNewDocumentAsync("Object.defineProperty(navigator, 'webdriver', {get: () => undefined})");

                await page.SetUserAgentAsync("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/121.0.0.0 Safari/537.36");

                // 2. Sahifaga boramiz
                var response = await page.GoToAsync("https://license.gov.uz/registry/591c96fe-de93-4f1d-8d26-c1c5974cade3",
                    new NavigationOptions { WaitUntil = new[] { WaitUntilNavigation.Load }, Timeout = 60000 });

                // Agar sayt bizni bloklasa, status kodini tekshiramiz
                if (response.Status == System.Net.HttpStatusCode.InternalServerError)
                {
                    return Json(new { success = false, message = "Sayt bizni blokladi (500 xatosi)" });
                }

                // 3. Oynacha yuklanishini kutamiz (Vaqtni 15 soniya qilamiz, zerikib qolmaslik uchun)
                await Task.Delay(15000);

                // 4. Matnni almashtiramiz
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

                // 5. HTMLni olamiz
                string fullHtml = await page.GetContentAsync();

                // 6. SCRIPTlarni KOMMENTGA olamiz (Redirect bo'lmasligi uchun)
                // Bu localhostda qochib ketishni 100% to'xtatadi
                fullHtml = fullHtml.Replace("<script", "");

                // 7. Bazaviy URL
                fullHtml = fullHtml.Replace("<head>", "<head><base href='https://license.gov.uz/'>");

                return Json(new { success = true, data = fullHtml });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Xatolik: " + ex.Message });
            }
        }
    }
}
