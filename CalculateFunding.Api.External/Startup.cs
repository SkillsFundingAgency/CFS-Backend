using System;
using System.IO;
using System.Linq;
using System.Reflection;
using CalculateFunding.Api.External.Swagger.OperationFilters;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.PlatformAbstractions;
using Newtonsoft.Json;
using Swashbuckle.AspNetCore.Examples;
using Swashbuckle.AspNetCore.Swagger;

namespace CalculateFunding.Api.External
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc(options =>
            {
                options.OutputFormatters.RemoveType<StringOutputFormatter>();
                options.OutputFormatters.Add(new XmlSerializerOutputFormatter());

                var jFormatter =
                    options.OutputFormatters.FirstOrDefault(f => f.GetType() == typeof(JsonOutputFormatter)) as
                        JsonOutputFormatter;
                jFormatter?.SupportedMediaTypes.Clear();
                jFormatter?.SupportedMediaTypes.Add("text/plain");
                jFormatter?.SupportedMediaTypes.Add("application/vnd.sfa.allocation.1+json");
                jFormatter?.SupportedMediaTypes.Add("application/vnd.sfa.allocation.1+atom+json");

                var xFormatter =
                    options.OutputFormatters.FirstOrDefault(f => f.GetType() == typeof(XmlSerializerOutputFormatter)) as
                        XmlSerializerOutputFormatter;
                xFormatter?.SupportedMediaTypes.Clear();
                xFormatter?.SupportedMediaTypes.Add("application/vnd.sfa.allocation.1+xml");
                xFormatter?.SupportedMediaTypes.Add("application/vnd.sfa.allocation.1+atom+xml");
            }).AddJsonOptions(options => { options.SerializerSettings.Formatting = Formatting.Indented; });

            //.SetCompatibilityVersion(CompatibilityVersion.Version_2_1);

            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new Info
                {
                    Title = "Calculate Funding Service API",
                    Version = "v1",
                    Description = @"
# Calculate Funding Service 
# Service Context
### Purpose of the Service
The Calculate Funding Service manages the specification, calculation, testing and publishing of provider allocations. It's main responsibilities include:

  * Specifying funding policies, linked to funding streams and allocation lines
  * Defining calculations that implement specified policies
  * Defining and importing datasets that provider source data required by the calculations
  * Supporting the creation and execution of test scenarios that validate the calculation results
  * Supporting the publishing of correct allocations through viewing the results of tests and calculations
  * Providing an API that allows external systems to obtain details of published allocations

    
## Governance
TBC

## Usage Scope
This Service is designed for internal Agency use only.

## Availability
TBC

## Performance and Scalability
TBC

## Pre-Requisites
* Client must have network access to the calculate funding api
* Client must have the necessary credentials to access the API
* Client must trust the server certificate used for SSL connections.

## Post-Requisites
* None

## Media Types
The following table lists the media types used by the service:

| Media Type    | Description |
| ------------- |-------------| 
| application/vnd.sfa.allocation.{VERSION}+xml | An allocation in XML format |
| application/vnd.sfa.allocation.{VERSION}+json | An allocation in JSON format  |
| application/vnd.sfa.allocation.{VERSION}+atom+xml | An atom feed representing a stream of allocations in XML format. Each content item in the feed will be vnd.sfa.allocation.{VERSION} |
| application/vnd.sfa.allocation.{VERSION}+atom+json | An atom feed representing a stream of allocations in JSON format.* Each content item in the feed will be vnd.sfa.allocation.{VERSION}  |

* This not a part of the ATOM standard but is a convenience feature for native JSON clients.
The media Type above conform to the Accept Header specification. In simple terms that states that the media Type is vendor specific, is a given representation (sfa.allocation and version) and delivered in a particular wire format (JSON or XML).

## Request Headers
The following HTTP headers are supported by the service.

| Header | Optionality    | Description |
| ------------- |-------------|---------------| 
| Accept     | Required | The calculate funding service uses the Media Type provided in the Accept header to determine what representation of a particular resources to serve. In particular this includes the version of the resource and the wire format. |

## Response Header
There are no custom headers returned by the service. However each call will return header to aid the client with caching responses.

## Page of Results
The calculate funding API provides resources that represent notification streams. These will be provided as an ATOM feed. The ATOM specification makes provision for paging results in two ways. Both methods provides a means for client to navigate the stream without prior knowledge of the necessary URIs. This follows a Hypermedia As The Engine Of Application State (HATEAOS) pattern. The selected paging method will depend on the specific resource. Clients are expected to be tolerant to changes to the paging method within the confines of the ATOM specification 

## Generic Error and Exception Behaviour
All operations will return one of the following messages in the event a generic exception is encountered.

| Error | Description    | Should Retry |
| ------------- |-------------|---------------| 
| 401 Unauthorized | The consumer is not authorized to use this service. | No |
| 404 Not Found  | The resource requested cannot be found. Check the structure of the URI  | No |
| 410 Gone | The requested resource is no longer available at the server and no forwarding address is known. This will be used when a previously supported resource URI has been depreciated.  | No
| 415 Unsupported Media Type |  The media Type provide in the Accept header is not supported by the server. This error will also be produced if the client requests a version of a media Type not supported by the service. | No
| 500 Internal Server Error | The service encountered an unexpected error. The service may have logged an error, and if possible the body will contain a text/plain guid that can be used to search the logs. | Unknown, so limited retries may be attempted
| 503 Service Unavailable |  The service is too busy | Yes
",

                    //# Allocation Notifications 

                    //Resources related to Allocation Notifications

                    //## Allocation Approvals

                    //Provides a stream of approved allocations where approval is defined as the allocation being approved for payment. The stream will be paged using the paging scheme detailed below. If the pageref is omitted the most recent page of results will be returned.

                    //If the client requests a specific version of the allocation this will be the representation used within the Atom Feed. If the client simply requests atom, the service will use the latest version of the most appropriate media Type, in this case sfa.allocation. Without an explicit media Type there is a risk that the client will receive a version of the allocation that it was not expecting which could cause undesirable side effects. Therefore it is recommended that clients provide explicit media types. 

                    //In all cases the atom:feed\atom:entry\atom:content\@Type attribute in the response payload will be populated with the appropriate media Type for the content held within the atom:feed\atom:entry\atom:content element. This will be application/vnd.sfa.allocation.{VERSION}+xml or application/vnd.sfa.allocation.{VERSION}+json as related to the requested media Type or the latest version if the client does not explicitly provide it.

                    //Paging:

                    //The Approved Allocation notification stream will be an Archived Feed as described in the ATOM Feed Paging and Archive specification. As allocation approvals are unlikely to be scattered evenly across time and instead bunched around particular business dates, archived pages will be a fixed sized and addressed via a specified page reference.  The server will determine the page size.
                    //",
                    Contact = new Contact
                    {
                        Name = "Clifford Smith",
                        Email = "cliffordsmith@education.gov.uk"
                    },
                    License = new License
                    {
                        Name = "MIT License",
                        Url = "https://opensource.org/licenses/MIT"
                    }
            });
                c.AddSecurityDefinition("apiKey", new ApiKeyScheme
                {
                    Type = "apiKey",
                    Name = "x-api-key",
                    In = "header"
                });
                c.OperationFilter<AddResponseHeadersFilter>();
                c.OperationFilter<AddRequiredHeaderParameters>();

                var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                c.IncludeXmlComments(xmlPath);

                var modelsFilePath = Path.Combine(PlatformServices.Default.Application.ApplicationBasePath,
                    "CalculateFunding.Models.xml");
                c.IncludeXmlComments(modelsFilePath);

                //c.OperationFilter<OperationFilterFactory>();
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
                app.UseDeveloperExceptionPage();
            //else
            //    app.UseHsts();

            //app.UseStaticFiles();
            //app.UseHttpsRedirection();
            app.UseMvc();
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.DocExpansion(DocExpansion.List);
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "My API V1");
                c.RoutePrefix = "docs";
            });
        }
    }
}