Example usage:

dotnet CalculateFunding.Migrations.Specification.Clone.dll --src-spec-id "3aa22644-6343-444f-a84d-395f76ed1682" --trg-period-id "AS-2223" --trg-funding-template-version "5.0" --include-released-data-dataset false

SpecificationMappingOptions section only required in 'include-released-data-dataset' flag is true.
targetRelationshipName and targetRelationshipDescription - are optional if you want to change the name of the released dataset in the target environment. You may
want to do this if you are referencing a different funding period in target.

## Below is a sample user secrets Config to run application using Visual Studio 2022.
{
  "source:specificationsClient:ApiEndpoint": "https://localhost:7001/api/",
  "source:specificationsClient:ApiKey": "Local",
  "source:calcsClient:ApiEndpoint": "https://localhost:7002/api/",
  "source:calcsClient:ApiKey": "Local",
  "source:policiesClient:ApiEndpoint": "https://localhost:7013/api/",
  "source:policiesClient:ApiKey": "Local",
  "source:jobsClient:ApiEndpoint": "https://localhost:7010/api/",
  "source:jobsClient:ApiKey": "Local",
  "source:datasetsClient:ApiEndpoint": "https://localhost:7004/api/",
  "source:datasetsClient:ApiKey": "Local",

  "target:specificationsClient:ApiEndpoint": "https://localhost:7001/api/",
  "target:specificationsClient:ApiKey": "Local",
  "target:calcsClient:ApiEndpoint": "https://localhost:7002/api/",
  "target:calcsClient:ApiKey": "Local",
  "target:policiesClient:ApiEndpoint": "https://localhost:7013/api/",
  "target:policiesClient:ApiKey": "Local",
  "target:jobsClient:ApiEndpoint": "https://localhost:7010/api/",
  "target:jobsClient:ApiKey": "Local",
  "target:datasetsClient:ApiEndpoint": "https://localhost:7004/api/",
  "target:datasetsClient:ApiKey": "Local",

  "SpecificationMappingOptions": [
    {
      "sourceSpecificationId": "d82cd692-f21f-44b3-8218-b7a97824826d",
      "targetSpecificationId": "c90bb424-db6e-4ed2-a53e-fae95c6611ef",
      "targetRelationshipName": "SpecToSpec Dataset",
      "targetRelationshipDescription": "Previous years spec"
    }
  ],

  "ApplicationInsightsOptions": {
    "InstrumentationKey": "91f63fdd-c808-4f01-bf21-73c8616b2bdc"
  }
}