using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;

namespace AstreaProcessAutomator
{
    class Program
    {
        private static IWebDriver? driver;
        private static string logFilePath = "automation_log.txt";
        private static string processNumbersFilePath = "process_numbers.txt";
        private static string processResultsFilePath = "process_results.txt";

        static void Main(string[] args)
        {
            try
            {
                //RegisterProcessNumbersInAstrea();
                CheckTheProcessAndAddToAstrea();
            }
            catch (Exception ex)
            {
                LogMessage($"Erro geral: {ex.Message}");
            }
            finally
            {
                driver?.Quit();
            }

            Console.WriteLine("Pressione qualquer tecla para sair...");
            Console.ReadKey();
        }

        private static void CheckTheProcessAndAddToAstrea()
        {
            InitializeLog();
            SetupWebDriver();
            if (Login())
            {
                if (driver == null)
                {
                    LogMessage("Driver não foi inicializado.");
                    return;
                }

                IJavaScriptExecutor jsExecutor = (IJavaScriptExecutor)driver;
                ICollection<IWebElement> processos;

                do
                {
                    driver.Navigate().GoToUrl("https://astrea.net.br/#/main/workspace/%5B,%5D");
                    Thread.Sleep(2000);

                    driver
                        .Navigate()
                        .GoToUrl("https://astrea.net.br/#/main/pending-search/search-results");
                    Thread.Sleep(2000);

                    // Clica para ordenar por status
                    jsExecutor.ExecuteScript(
                        "document.getElementsByClassName('au-table-list__caret ng-binding')[2].click();"
                    );

                    processos = driver
                        .FindElement(By.ClassName("au-table-list"))
                        .FindElements(By.ClassName("ng-scope"));

                    LogMessage($"Encontrados {processos.Count} processos para processar.");

                    foreach (var processo in processos)
                    {
                        try
                        {
                            processo.Click();
                        }
                        catch
                        {
                            continue;
                        }

                        Thread.Sleep(1000);

                        try
                        {
                            // Clica na etiqueta
                            var dropdownButton = processo
                                .FindElements(By.ClassName("dropdown-header"))[0]
                                .FindElement(By.TagName("i"));
                            dropdownButton.Click();

                            Thread.Sleep(500);
                            // Seleciona "CASOS RHILO"
                            jsExecutor.ExecuteScript(
                                "document.querySelector(\"#mainDiv > div.au-app-access > div > div > main > pending-search > div > pending-search-results > div > div > div:nth-child(3) > div > pending-search-lawsuit > div:nth-child(2) > div > div > table > tbody:nth-child(2) > tr.ng-scope > td > pending-search-info > div > div > div.row.nix-padding-top_30 > div.col-xs-6.nix-padding-left_30.nix-padding-right_30.au-search-info__border-right > form > div:nth-child(1) > div > ul > li > div > div.dropdown-body > ul > li:nth-child(5)\").click()"
                            );
                            Thread.Sleep(300);
                            // Clica em inserir
                            jsExecutor.ExecuteScript(
                                "document.querySelector(\"#mainDiv > div.au-app-access > div > div > main > pending-search > div > pending-search-results > div > div > div:nth-child(3) > div > pending-search-lawsuit > div:nth-child(2) > div > div > table > tbody:nth-child(2) > tr.ng-scope > td > pending-search-info > div > div > div.row.nix-margin-bottom_15 > div > button\").click();"
                            );

                            Thread.Sleep(3000);
                        }
                        catch (Exception ex)
                        {
                            LogMessage($"Erro: {ex.Message}");
                            continue;
                        }
                    }

                    LogMessage(
                        "Foreach finalizado. Voltando para verificar se há mais processos..."
                    );
                } while (processos.Count > 0);

                LogMessage("Não há mais processos para processar. Loop finalizado.");
            }
        }

        private static void RegisterProcessNumbersInAstrea()
        {
            InitializeLog();
            var processNumbers = ReadProcessNumbers();
            if (processNumbers.Count == 0)
            {
                LogMessage("Nenhum número de processo encontrado no arquivo.");
                return;
            }
            SetupWebDriver();
            if (Login())
            {
                foreach (string processNumber in processNumbers)
                {
                    ProcessNumber(processNumber);
                    Thread.Sleep(2000);
                }
            }

            LogMessage("Automação concluída com sucesso.");
        }

        private static void InitializeLog()
        {
            string logHeader = $"=== LOG DE AUTOMAÇÃO - {DateTime.Now:dd/MM/yyyy HH:mm:ss} ===\n";
            File.WriteAllText(logFilePath, logHeader);
            LogMessage("Sistema de automação iniciado.");
            if (!File.Exists(processResultsFilePath))
            {
                File.WriteAllText(
                    processResultsFilePath,
                    $"=== RESULTADOS DOS PROCESSOS - {DateTime.Now:dd/MM/yyyy HH:mm:ss} ===\n"
                );
            }
        }

        private static List<string> ReadProcessNumbers()
        {
            List<string> numbers = new List<string>();

            try
            {
                if (File.Exists(processNumbersFilePath))
                {
                    string[] lines = File.ReadAllLines(processNumbersFilePath);
                    foreach (string line in lines)
                    {
                        string trimmedLine = line.Trim();
                        if (!string.IsNullOrEmpty(trimmedLine))
                        {
                            numbers.Add(trimmedLine);
                        }
                    }
                    LogMessage($"Carregados {numbers.Count} números de processo do arquivo.");
                }
                else
                {
                    LogMessage($"Arquivo {processNumbersFilePath} não encontrado.");
                    CreateSampleProcessFile();
                }
            }
            catch (Exception ex)
            {
                LogMessage($"Erro ao ler arquivo de processos: {ex.Message}");
            }

            return numbers;
        }

        private static void CreateSampleProcessFile()
        {
            try
            {
                string[] sampleNumbers = { "123456789", "987654321", "555666777" };

                File.WriteAllLines(processNumbersFilePath, sampleNumbers);
                LogMessage($"Arquivo de exemplo criado: {processNumbersFilePath}");
            }
            catch (Exception ex)
            {
                LogMessage($"Erro ao criar arquivo de exemplo: {ex.Message}");
            }
        }

        private static void SetupWebDriver()
        {
            try
            {
                ChromeOptions options = new ChromeOptions();
                options.AddArgument("--start-maximized");
                options.AddArgument("--disable-blink-features=AutomationControlled");
                options.AddExcludedArgument("enable-automation");
                options.AddAdditionalOption("useAutomationExtension", false);

                driver = new ChromeDriver(options);
                driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(10);

                LogMessage("WebDriver configurado com sucesso.");
            }
            catch (Exception ex)
            {
                LogMessage($"Erro ao configurar WebDriver: {ex.Message}");
                throw;
            }
        }

        private static bool Login()
        {
            if (driver == null)
            {
                LogMessage("Driver não foi inicializado.");
                return false;
            }

            try
            {
                LogMessage("Iniciando processo de login...");
                string loginUrl = "https://astrea.net.br/#/login/";
                driver.Navigate().GoToUrl(loginUrl);
                LogMessage($"Navegando para: {loginUrl}");

                WebDriverWait wait = new WebDriverWait(driver, TimeSpan.FromSeconds(15));

                var user = "alterar para o usuário correto";
                IWebElement usernameField = wait.Until(d => d.FindElement(By.Name("username")));
                usernameField.SendKeys(user);

                var password = "alterar para a senha correta";
                IWebElement passwordField = driver.FindElement(By.Name("password"));
                passwordField.SendKeys(password);

                IWebElement loginButton = driver.FindElement(By.TagName("button"));

                LogMessage("Credenciais preenchidas.");

                loginButton.Click();
                LogMessage("Botão de login clicado.");
                Thread.Sleep(10000);

                if (driver.Url.Contains("workspace"))
                {
                    LogMessage("Login realizado com sucesso.");
                    return true;
                }
                else
                {
                    LogMessage("Falha no login - página não redirecionada corretamente.");
                    return false;
                }
            }
            catch (Exception ex)
            {
                LogMessage($"Erro durante login: {ex.Message}");
                return false;
            }
        }

        private static void ProcessNumber(string processNumber)
        {
            if (driver == null)
            {
                LogMessage("Driver não foi inicializado.");
                SaveProcessResult(processNumber, "Erro: Driver não inicializado");
                return;
            }

            try
            {
                LogMessage($"Processando número: {processNumber}");

                string processUrl = "https://astrea.net.br/#/main/pending-search/cnj-form";
                driver.Navigate().GoToUrl(processUrl);
                LogMessage($"Navegando para página de processos: {processUrl}");

                WebDriverWait wait = new WebDriverWait(driver, TimeSpan.FromSeconds(15));

                IWebElement processField = wait.Until(d => d.FindElement(By.Id("cnjSearchInput")));
                processField.Clear();
                processField.SendKeys(processNumber);
                LogMessage($"Número {processNumber} inserido no campo.");

                IWebElement insertButton = driver.FindElements(By.TagName("button"))[8];
                insertButton.Click();
                Thread.Sleep(3000);

                IWebElement searchButton = driver.FindElements(By.TagName("button"))[10];
                searchButton.Click();
                Thread.Sleep(5000);

                string response = GetPortalResponseWithJS();
                SaveProcessResult(processNumber, response);

                LogMessage($"Processo {processNumber} finalizado com resposta: {response}");
            }
            catch (Exception ex)
            {
                LogMessage($"Erro ao processar {processNumber}: {ex.Message}");
                SaveProcessResult(processNumber, $"Erro: {ex.Message}");
            }
        }

        private static string GetPortalResponseWithJS()
        {
            if (driver == null)
            {
                LogMessage("Driver não foi inicializado para capturar resposta.");
                return "Erro: Driver não inicializado";
            }

            try
            {
                IJavaScriptExecutor jsExecutor = (IJavaScriptExecutor)driver;
                var result = jsExecutor.ExecuteScript(
                    "var element = document.getElementsByClassName('css-1h8dbg8-TextElement efb112z0')[0]; "
                        + "return element ? element.innerText : null;"
                );

                string? responseText = result?.ToString();
                if (string.IsNullOrEmpty(responseText) || string.IsNullOrWhiteSpace(responseText))
                {
                    return "inserido com sucesso";
                }

                return responseText.Trim();
            }
            catch (Exception ex)
            {
                LogMessage($"Erro ao capturar resposta com JavaScript: {ex.Message}");
                return "inserido com sucesso";
            }
        }

        private static void SaveProcessResult(string processNumber, string response)
        {
            try
            {
                string resultLine = $"{processNumber} - {response}\n";
                File.AppendAllText(processResultsFilePath, resultLine);
                LogMessage($"Resultado salvo: {processNumber} - {response}");
            }
            catch (Exception ex)
            {
                LogMessage($"Erro ao salvar resultado do processo {processNumber}: {ex.Message}");
            }
        }

        private static void LogMessage(string message)
        {
            string timestamp = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");
            string logEntry = $"[{timestamp}] {message}\n";

            try
            {
                File.AppendAllText(logFilePath, logEntry);
                Console.WriteLine(logEntry.TrimEnd('\n'));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao escrever log: {ex.Message}");
            }
        }
    }
}
