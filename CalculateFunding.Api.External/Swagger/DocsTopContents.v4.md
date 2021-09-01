# Calculate Funding Service
# Service Context
### Purpose of the Service
The Calculate Funding Service manages the specification, calculation, testing and publishing of funding. It's main responsibilities include:
* Providing an API that allows external systems to obtain details of published funding

    
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
| application/json | An allocation in JSON format  |
| application/atom+json | An atom feed representing a stream of funding in JSON format.* Each content item in the feed will be the PublishedProviderVersion  |

* This not a part of the ATOM standard but is a convenience feature for native JSON clients.
The media Type above conform to the Accept Header specification. In simple terms that states that the media Type is vendor specific, is a given representation (sfa.funding and version) and delivered in a particular wire format (JSON).

## Request Headers
The following HTTP headers are supported by the service.

| Header | Optionality    | Description |
| ------------- |-------------|---------------| 
| Accept     | Required | The calculate funding service uses the Media Type provided in the Accept header to determine what representation of a particular resources to serve. In particular this includes the version of the resource and the wire format. |

## Response Header
There are no custom headers returned by the service. However each call will return header to aid the client with caching responses.

## Page of Results
The paging follows the RFC 5005 specification.

See https://tools.ietf.org/html/rfc5005 for more information. **Section 4. Archived Feeds** explains the behaviour.

The first page of the notification feed contains the latest items, up to when the page size is hit.

Then immutable historical pages are created in date descending order, eg page 4 contains newer items than page 3.

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

## What is breaking change in API (REST)?
- Change in Input parameters
- Change (Removals of Attributes) in existing Data Model returned from API
- Change (Re-purpose of existing Attributes) in existing Data Model returned from API
- Changing Pagination order

## What is non breaking change in API (REST)?
- Introducing additional optional parameters
- Introducing new attributes in the Data Model

## Changes since V1
  - Added additional filters to AllocationNotification API
  - Removal of allocationStatuses from URL of AllocationNotification API (now querystring parameter)
  - Removed internal versioning (integer versioning) from API - client should now use major and minor versions
  - Add provider variation details to ProviderSummary. See model documentation for more details.
  - Added Financial Envelopes to allocation line result APIs