using System;
using Microsoft.Xrm.Tooling.Connector;
using Microsoft.Xrm.Sdk.Client;
using Microsoft.Xrm.Sdk;
using System.ServiceModel;
using Microsoft.Xrm.Sdk.Query;

namespace LoginDynamics
{
    internal class Program : IPlugin
    {
        static void Main(string[] args)
        {
            // Defina as informações de autenticação
            string username = "";
            string password = "";
            string url = "";

            // Crie a string de conexão com o Dynamics 365 usando autenticação baseada em usuário
            var connectionString = $@"
                AuthType=Office365;
                Url={url};
                Username={username};
                Password={password}";

            // Conecte-se ao Dynamics 365
            CrmServiceClient serviceClient = new CrmServiceClient(connectionString);

            // Verifique se a conexão foi bem-sucedida
            if (!serviceClient.IsReady)
            {
                Console.WriteLine($"Erro ao conectar: {serviceClient.LastCrmError}");
                return;
            }

            // Crie um contexto de organização que será usado para executar as operações
            IOrganizationService orgService = (IOrganizationService)serviceClient.OrganizationWebProxyClient ?? serviceClient.OrganizationServiceProxy;

            // Aqui você pode executar qualquer operação que gostaria de depurar. Este é apenas um exemplo.
            try
            {
                string nomeDaTarefa = "Criação do banco de dados";

                // Montagem da query
                QueryExpression queryProcuraTarefa = new QueryExpression("crb40_tarefa");
                // Adicionando colunas
                queryProcuraTarefa.ColumnSet.AddColumns("crb40_nome", "crb40_projeto", "crb40_status");
                // Adicionando condições
                queryProcuraTarefa.Criteria.AddCondition("crb40_nome", ConditionOperator.Equal, nomeDaTarefa);
                // Adicionando relacionamento
                LinkEntity tabelaProjeto = queryProcuraTarefa.AddLink("crb40_projeto", "crb40_projeto", "crb40_projetoid");
                tabelaProjeto.EntityAlias = "tabelaProjeto";
                tabelaProjeto.Columns.AddColumns("crb40_nome", "crb40_status", "crb40_projetoid");
                // Executar a consulta para obter os resultados
                EntityCollection tarefaEncontrada = orgService.RetrieveMultiple(queryProcuraTarefa);

                // Verificar se a consulta retornou alguma tarefa
                if (tarefaEncontrada.Entities.Count > 0)
                {
                    foreach (Entity entity in tarefaEncontrada.Entities)
                    {
                        // Recuperar o valor das colunas desejadas
                        Guid projetoId = new Guid(entity.GetAttributeValue<AliasedValue>("tabelaProjeto.crb40_projetoid").Value.ToString());
                        string projetoNome = entity.GetAttributeValue<AliasedValue>("tabelaProjeto.crb40_nome").Value.ToString();

                        // Utilizar o valor recuperado conforme necessário
                        Entity projeto = new Entity("crb40_projeto", projetoId);

                        // Motagem query
                        QueryExpression queryVerificaTodasTaksEncerradas = new QueryExpression("crb40_tarefa");
                        // Adicionando condições
                        queryVerificaTodasTaksEncerradas.Criteria.AddCondition("crb40_projeto", ConditionOperator.Equal, projetoId);
                        queryVerificaTodasTaksEncerradas.Criteria.AddCondition("crb40_status", ConditionOperator.NotEqual, new OptionSetValue(2));
                        EntityCollection tasks = orgService.RetrieveMultiple(queryVerificaTodasTaksEncerradas);

                        if (tasks.Entities.Count == 0)
                        {
                            projeto["crb40_status"] = new OptionSetValue(2);
                            orgService.Update(projeto);
                        }
                    }
                }
                else
                {
                }
            }
            catch (FaultException<OrganizationServiceFault> ex)
            {
            }
        }

        public void Execute(IServiceProvider serviceProvider)
        {
            throw new NotImplementedException();
        }
    }
    }
