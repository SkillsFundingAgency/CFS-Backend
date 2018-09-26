<Description(Description:="Current Provider")>
Public Class Provider
    <Field(Id:="Name", Name:="Name")>
    <Description(Description:="The name of this provider as recorded in Get Information about Schools attribute 'Name'")>
    Public Property Name() As String

    <Field(Id:="DateOpened", Name:="DateOpened")>
    <Description(Description:="The date that this provider entity was opened as recorded in Get Information about Schools attribute 'OpenDate'")>
    Public Property DateOpened() As Date?

    <Field(Id:="ProviderType", Name:="ProviderType")>
    <Description(Description:="The top level description of the type of provider.  Get Information about Schools attribute 'EstablishmentTypeGroup (name)'")>
    Public Property ProviderType() As String

    <Field(Id:="ProviderSubType", Name:="ProviderSubType")>
    <Description(Description:="The low level description of the providers type. Get Information about schools attribute 'TypeOfEstablishment (name)'")>
    Public Property ProviderSubType() As String

    <Field(Id:="UKPRN", Name:="UKPRN")>
    <Description(Description:="The UK provider reference number as recorded from the providers registration on the UK Register of Learning Providers. Get Information About Schools attribute name 'UKPRN'")>
    Public Property UKPRN() As String

    <Field(Id:="URN", Name:="URN")>
    <Description(Description:="The unique reference number, the provider identifier from Get Information about schools attribute 'URN'")>
    Public Property URN() As String

    <Field(Id:="UPIN", Name:="UPIN")>
    <Description(Description:="The Unique Provider Identification Number, the main provider reference number in the Provider Infomation Management System (PIMS) (NO CURRENT SOURCE)")>
    Public Property UPIN() As String

    <Field(Id:="DfeEstablishmentNumber", Name:="DfeEstablishmentNumber")>
    <Description(Description:="The four digit establishment number for a school registered by the Department of Education. Get Information About Schools attribute 'EstablishmentNumber'")>
    Public Property DfeEstablishmentNumber() As String

    <Field(Id:="EstablishmentNumber", Name:="EstablishmentNumber")>
    <Description(Description:="The Local Authority Establishment Number, as a combination of the providers establishment number and Local authority code. Concatenated from Get Information About Schools attributes LA (code) and EstablishmentNumber")>
    Public Property EstablishmentNumber() As String

    <Field(Id:="LegalName", Name:="LegalName")>
    <Description(Description:="The providers legal entity name as recorded on UK Register of Learning Providers and OR Companies House (NO CURRENT SOURCE)")>
    Public Property LegalName() As String

    <Field(Id:="Authority", Name:="Authority")>
    <Description(Description:="The name of the local authority that the provider is located from Get Information About Schools attribute LA (name)")>
    Public Property Authority() As String

    <Field(Id:="DateClosed", Name:="DateClosed")>
    <Description(Description:="The date this provider ceased to be a provider of education or skills. Get Information About Schools attribute CloseDate")>
    Public Property DateClosed() As Date?

    <Field(Id:="LACode", Name:="LACode")>
    <Description(Description:="The three digit code from the Local Authrority  pre 2011 Office of National Statistics coding system. Get Information About Schools attribute LA (code)")>
    Public Property LACode() As String

    <Field(Id:="CrmAccountId", Name:="CrmAccountId")>
    <Description(Description:="The Account number of the provider on the Department Of Educations CRM (NO CURRENT SOURCE)")>
    Public Property CrmAccountId() As String

    <Field(Id:="NavVendorNo", Name:="NavVendorNo")>
    <Description(Description:="The account number of the provider on the Department Of Educations finance system (NO CURRENT SOURCE)")>
    Public Property NavVendorNo() As String

    <Field(Id:="Status", Name:="Status")>
    <Description(Description:="The current status of provider as a deliverer of education and skills from Get Information about schools attribute EstablishmentStatus (name)")>
    Public Property Status() As String

    <Field(Id:="PhaseOfEducation", Name:="PhaseOfEducation")>
    <Description(Description:="The stage of education delivered by the provider in the UK's five stage education system. Get Information About Schools attribute PhaseOfEducation (name)")>
    Public Property PhaseOfEducation() As String
End Class