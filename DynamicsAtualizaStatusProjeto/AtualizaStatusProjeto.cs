using System;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace DynamicsAtualizaStatusProjeto
{
    public class AtualizaStatusProjeto : IPlugin
    {
        public void Execute(IServiceProvider serviceProvider)
        {
            try
            {
                IPluginExecutionContext context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));
                IOrganizationServiceFactory serviceFactory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
                IOrganizationService service = serviceFactory.CreateOrganizationService(context.UserId);

                if (context.InputParameters.Contains("Target") && context.InputParameters["Target"] is Entity)
                {
                    Entity task = (Entity)context.InputParameters["Target"];

                    Entity fullTask = service.Retrieve(task.LogicalName, task.Id, new ColumnSet("crb40_nome"));

                    var nomeDaTarefa = fullTask["crb40_nome"];

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
                    EntityCollection tarefaEncontrada = service.RetrieveMultiple(queryProcuraTarefa);

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
                            queryVerificaTodasTaksEncerradas.Criteria.AddCondition("crb40_status", ConditionOperator.NotEqual, 2);
                            EntityCollection tasks = service.RetrieveMultiple(queryVerificaTodasTaksEncerradas);

                            if (tasks.Entities.Count == 0)
                            {
                                projeto["crb40_status"] = new OptionSetValue(2);
                                service.Update(projeto);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new InvalidPluginExecutionException(ex.Message);
            }
        }
    }
}
