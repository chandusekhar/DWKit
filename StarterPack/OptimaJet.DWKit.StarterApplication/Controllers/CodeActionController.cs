using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using OptimaJet.DWKit.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OptimaJet.DWKit.Core.View;
using OptimaJet.DWKit.Core.Model;
using System.Dynamic;
using Newtonsoft.Json.Linq;
using OptimaJet.DWKit.Core.CodeActions;

namespace OptimaJet.DWKit.StarterApplication.Controllers
{
    //[Authorize(IdentityServer4.IdentityServerConstants.LocalApi.PolicyName)]
    [Authorize]
    public class CodeActionController : Controller
    {
        [HttpPost]
        [Route("actions/execute")]
        public async Task<ActionResult> Execute(string name, string request)
        {
            try
            {
                var dictionary = JsonConvert.DeserializeObject<Dictionary<string, object>>(request);

                object objectRequest;

                if (dictionary.ContainsKey("data") && dictionary.ContainsKey("formName"))
                {
                    var formName = dictionary["formName"].ToString();

                    var model = await MetadataToModelConverter.GetEntityModelByFormAsync(formName, new BuildModelOptions(ignoreNameCase: true, strategy: BuildModelStartegy.ForGet))
                        .ConfigureAwait(false);

                    dictionary.TryGetValue("parameters", out var parameters);
                    dictionary.TryGetValue("modalId", out var modalId);

                    if (parameters is JObject parametersJObject)
                    {
                        var parametersDictionary = JsonConvert.DeserializeObject<Dictionary<string, object>>(parametersJObject.ToString());
                        parameters = new DynamicEntity(parametersDictionary);
                    }

                    objectRequest = new ServerCodeActionRequest
                    {
                        Data = new DynamicEntityDeserializer(model).DeserializeSingle(dictionary["data"].ToString()),
                        FormName = formName,
                        Parameters = parameters,
                        ModalId = modalId?.ToString()
                    };

                }
                else
                {
                    objectRequest = new DynamicEntity(dictionary);
                }

                var actionResult = await DWKitRuntime.ServerActions.ExecuteActionAsync(name, objectRequest).ConfigureAwait(false);

                if (actionResult is DynamicEntity de)
                {
                    return Json(new SuccessResponse(new {data = de.ToDictionary()}));
                }

                return Json(new SuccessResponse(actionResult));

            }
            catch (Exception e)
            {
                return Json(new FailResponse(e));
            }
        }
    }
}
