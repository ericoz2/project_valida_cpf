using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace httpValidaCpf
{
    public static class fnvalidacpf
    {
        [FunctionName("fnvalidacpf")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("Iniciando a validação do CPF.");

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            
            // Deserializando JSON para um objeto CpfRequest
            CpfRequest data = JsonConvert.DeserializeObject<CpfRequest>(requestBody);

            if (data?.Cpf == null)
            {
                return new BadRequestObjectResult("Por favor, informe um CPF válido.");
            }

            string cpf = data.Cpf;

            if (!ValidaCPF(cpf))
            {
                return new BadRequestObjectResult("CPF inválido.");
            }

            return new OkObjectResult("CPF válido e não consta na base de débitos.");
        }

        // Classe para deserializar o JSON de entrada
        public class CpfRequest
        {
            public string Cpf { get; set; }
        }

        public static bool ValidaCPF(string cpf)
        {
            if (string.IsNullOrEmpty(cpf))
                return false;

            // Remover caracteres não numéricos
            cpf = new string(cpf.Where(char.IsDigit).ToArray());

            if (cpf.Length != 11)
                return false;

            // Verificar se todos os números são iguais (ex: 111.111.111-11 é inválido)
            if (cpf.Distinct().Count() == 1)
                return false;

            int[] multiplicador1 = { 10, 9, 8, 7, 6, 5, 4, 3, 2 };
            int[] multiplicador2 = { 11, 10, 9, 8, 7, 6, 5, 4, 3, 2 };

            string tempCpf = cpf.Substring(0, 9);
            int soma = tempCpf.Select((t, i) => (t - '0') * multiplicador1[i]).Sum();

            int resto = soma % 11;
            int primeiroDigito = resto < 2 ? 0 : 11 - resto;

            tempCpf += primeiroDigito;
            soma = tempCpf.Select((t, i) => (t - '0') * multiplicador2[i]).Sum();

            resto = soma % 11;
            int segundoDigito = resto < 2 ? 0 : 11 - resto;

            return cpf.EndsWith($"{primeiroDigito}{segundoDigito}");
        }
    }
}
