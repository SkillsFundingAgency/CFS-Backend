Imports System

Public Class Calculations
    Inherits BaseCalculation

    Public Property Datasets As Datasets

    <Calculation(Id:="d5f641cb2a634618b803117390cad833")>
    <CalculationSpecification(Id:="P004_PriRate", Name:="P004_PriRate")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="bb411f3c107a40a8a71ff57604296e0e", Name:="Basic Entitlement")>
    Public Function P004_PriRate As Decimal
        Dim result As Decimal = 0
        Dim acadfilter As Decimal = F100_AllAcademies
        Dim FundingBasis As String = Datasets.AcademyInformation.FundingBasis
        Dim P004_PriRate As Decimal = LAtoProv(Datasets.APTProformadataset.BasicEntitlementPrimaryAmountPerPupil)
        If FundingBasis = "Place" Then
            exclude(rid)
        Else
            If acadfilter = 17181 Or acadfilter = 1712 Or acadfilter = 17183 Then
                result = P004_PriRate
            Else
                exclude(rid)
            End If
        End If

        Return result
    End Function

    <Calculation(Id:="88bd354144974c57b6d584c0d4d90675")>
    <CalculationSpecification(Id:="P005_PriBESubtotal", Name:="P005_PriBESubtotal")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="bb411f3c107a40a8a71ff57604296e0e", Name:="Basic Entitlement")>
    Public Function P005_PriBESubtotal As Decimal
        Dim result = Decimal.Zero
        Dim F100_AllAcademies As Decimal = F100_AllAcademies
        Dim FundingBasis As String = Datasets.AcademyInformation.FundingBasis
        Dim PrimaryPupils As Decimal = NOR_P22_Total_NOR_PRI_SBS
        Dim P004_PriRate As Decimal = P004_PriRate
        Dim P005_Pri_BESubtotal As Decimal = PrimaryPupils * P004_PriRate
        Dim P005_Pri_BESubtotalAPT As Decimal = Datasets.APTNewISBdataset.BasicEntitlementPrimary
        Print(F100_AllAcademies, "F100_AllAcademies", rid)
        Print(FundingBasis, "FundingBasis", rid)
        Print(PrimaryPupils, "PrimaryPupils", rid)
        Print(P004_PriRate, "P004_PriRate", rid)
        Print(P005_Pri_BESubtotal, "P005_Pri_BESubtotal", rid)
        If FundingBasis = "Place" Then
            exclude(rid)
        Else
            If F100_AllAcademies = 17181 Or (F100_AllAcademies = 17182 And FundingBasis = "Estimate") Or (F100_AllAcademies = 17183 And FundingBasis = "Estimate") THEN
                Result = P005_Pri_BESubtotal
            Else
                If(F100_AllAcademies = 17182 And FundingBasis = "Census") Or (F100_AllAcademies = 17183 And FundingBasis = "Census") THEN
                    'Print(P005_Pri_BESubtotalAPT,"P005_Pri_BESubtotalAPT",rid)
                    Result = P005_Pri_BESubtotalAPT
                Else
                    exclude(rid)
                End If
            End If
        End If

        Return result
    End Function

    <Calculation(Id:="bfcd6d3694374effaeb28f2d3eecd5d7")>
    <CalculationSpecification(Id:="P006_NSEN_PriBE", Name:="P006_NSEN_PriBE")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="bb411f3c107a40a8a71ff57604296e0e", Name:="Basic Entitlement")>
    Public Function P006_NSEN_PriBE As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim P005_PriBESubtotal As Decimal = P005_PriBESubtotal
        Dim P006a_NSEN_PriBE_percent As Decimal = P006a_NSEN_PriBE_percent / 100
        Dim P006_NSEN_PriBE As Decimal = P005_PriBESubtotal * P006a_NSEN_PriBE_percent
        Dim acadfilter As Decimal = F100_AllAcademies
        Dim FundingBasis As String = Datasets.AcademyInformation.FundingBasis
        If FundingBasis = "Place" Then
            exclude(rid)
        Else
            If acadfilter = 17181 Or acadfilter = 17182 Or acadfilter = 17183 Then
                Result = P006_NSEN_PriBE
            Else
                exclude(rid)
            End If
        End If

        Return result
    End Function

    <Calculation(Id:="39dca1a7f9884a83873e14d11f589ade")>
    <CalculationSpecification(Id:="P006a_NSEN_PriBE_Percent", Name:="P006a_NSEN_PriBE_Percent")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="bb411f3c107a40a8a71ff57604296e0e", Name:="Basic Entitlement")>
    Public Function P006a_NSEN_PriBE_Percent As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="9a24096d1794405397edf7aeb0440af6")>
    <CalculationSpecification(Id:="P007_InYearPriBE_Subtotal", Name:="P007_InYearPriBE_Subtotal")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="bb411f3c107a40a8a71ff57604296e0e", Name:="Basic Entitlement")>
    Public Function P007_InYearPriBE_Subtotal As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim P005_PriBESubtotal As Decimal = P005_PriBESubtotal
        Dim P001_1718DaysOpen As Decimal = P001_1718DaysOpen
        Dim P25_YearDays_1718 As Decimal = P025_YearDays_1718
        Dim F200_SBS_Academies As Decimal = F200_SBS_Academies
        Print(P005_PriBESubtotal, "P005_PriBESubtotal", rid)
        Print(P001_1718DaysOpen, "P001_1718DaysOpen", rid)
        Print(P25_YearDays_1718, "P25_YearDays_1718", rid)
        Print(F200_SBS_Academies, "F200_SBS_Academies", rid)
        Result =(P005_PriBESubtotal) * P001_1718DaysOpen / P25_YearDays_1718
        Return result
    End Function

    <Calculation(Id:="e103d951e52e42f0bb8289e1702db277")>
    <CalculationSpecification(Id:="P009_KS3Rate", Name:="P009_KS3Rate")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="bb411f3c107a40a8a71ff57604296e0e", Name:="Basic Entitlement")>
    Public Function P009_KS3Rate As Decimal
        Dim result = Decimal.Zero
        Dim acadfilter As Decimal = F100_AllAcademies
        Dim FundingBasis As String = Datasets.AcademyInformation.FundingBasis
        Dim P009_KS3Rate As Decimal = LAtoProv(Datasets.APTProformadataset.BasicEntitlementKS3AmountPerPupil)
        If FundingBasis = "Place" Then
            exclude(rid)
        Else
            If acadfilter = 17181 Or acadfilter = 17182 Or acadfilter = 17183 Then
                result = P009_KS3Rate
            Else
                Exclude(rid)
            End If
        End If

        Return result
    End Function

    <Calculation(Id:="eec61e38ead7447a8dcc7d09f86bfd9e")>
    <CalculationSpecification(Id:="P010_KS3_BESubtotal", Name:="P010_KS3_BESubtotal")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="bb411f3c107a40a8a71ff57604296e0e", Name:="Basic Entitlement")>
    Public Function P010_KS3_BESubtotal As Decimal
        Dim result = Decimal.Zero
        Dim F100_AllAcademies As Decimal = F100_AllAcademies
        Dim FundingBasis As String = Datasets.AcademyInformation.FundingBasis
        Dim AcadFilter As Decimal = F200_SBS_Academies
        Dim P008_KS3Pupils As Decimal = NOR_P23_Total_NOR_KS3_SBS
        Dim P009_KS3Rate As Decimal = P009_KS3Rate
        Dim P010_KS3_BESubtotal As Decimal =(P008_KS3Pupils * P009_KS3Rate)
        Dim P010_KS3_BESubtotalAPT As Decimal = Datasets.APTNewISBdataset.BasicEntitlementKS3
        Print(F100_AllAcademies, "F100_AllAcademies", rid)
        Print(FundingBasis, "FundingBasis", rid)
        Print(P008_KS3Pupils, "P008_KS3Pupils", rid)
        Print(P009_KS3Rate, "P009_KS3Rate", rid)
        If FundingBasis = "Place" Then
            exclude(rid)
        Else
            If F100_AllAcademies = 17181 Or (F100_AllAcademies = 17182 And FundingBasis = "Estimate") Or (F100_AllAcademies = 17183 And FundingBasis = "Estimate") THEN
                Result = P010_KS3_BESubtotal
            Else
                If(F100_AllAcademies = 17182 And FundingBasis = "Census") Or (F100_AllAcademies = 17183 And FundingBasis = "Census") THEN
                    Print(P010_KS3_BESubtotalAPT, "P010_KS3_BESubtotalAPT", rid)
                    Result = P010_KS3_BESubtotalAPT
                Else
                    exclude(rid)
                End If
            End If
        End If

        Return result
    End Function

    <Calculation(Id:="cf0c84312b044b5da4e3fbeea455d469")>
    <CalculationSpecification(Id:="P011_NSEN_KS3BE_percent", Name:="P011_NSEN_KS3BE_percent")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="bb411f3c107a40a8a71ff57604296e0e", Name:="Basic Entitlement")>
    Public Function P011_NSEN_KS3BE_percent As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim acadfilter As Decimal = F100_AllAcademies
        Dim FundingBasis As String = Datasets.AcademyInformation.FundingBasis
        Dim P010_KS3_BESubtotal As Decimal = P010_KS3_BESubtotal
        Dim P011a_NSEN_KS3BE_percent As Decimal = P011a_NSEN_KS3BE_percent / 100
        Dim P011_NSEN_KS3BE As Decimal = P010_KS3_BESubtotal * P011a_NSEN_KS3BE_percent
        Print(P010_KS3_BESubtotal, "P010_KS3_BESubtotal", rid)
        Print(P011a_NSEN_KS3BE_percent, "P011a_NSEN_KS3BE_percent", rid)
        If FundingBasis = "Place" Then
            exclude(rid)
        Else
            If acadfilter = 17181 Or acadfilter = 17182 Or acadfilter = 17183 Then
                Result = P011_NSEN_KS3BE
            Else
                Exclude(rid)
            End If
        End If

        Return result
    End Function

    <Calculation(Id:="97ac3d4190cf4f7f8bdc882bd8790d22")>
    <CalculationSpecification(Id:="P011a_NSEN_KS3BE_Percent", Name:="P011a_NSEN_KS3BE_Percent")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="bb411f3c107a40a8a71ff57604296e0e", Name:="Basic Entitlement")>
    Public Function P011a_NSEN_KS3BE_Percent As Decimal
        Dim result = Decimal.Zero
        Dim acadfilter As Decimal = F100_AllAcademies
        Dim FundingBasis As String = Datasets.AcademyInformation.FundingBasis
        Dim NSEN_KS3BE_Percent As Decimal = LAtoProv(Datasets.APTProformadataset.BasicEntitlementKS3NotionalSEN)
        Dim P011a_NSEN_KS3BE_Percent As Decimal = NSEN_KS3BE_Percent * 100
        If FundingBasis = "Place" Then
            exclude(rid)
        Else
            If acadfilter = 17181 Or acadfilter = 17182 Or acadfilter = 17183 Then
                Result = P011a_NSEN_KS3BE_Percent
            Else
                Exclude(rid)
            End If
        End If

        Return result
    End Function

    <Calculation(Id:="53aaf7728382490885caec1e24c109df")>
    <CalculationSpecification(Id:="P012_InYearKS3_BESubtotal", Name:="P012_InYearKS3_BESubtotal")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="bb411f3c107a40a8a71ff57604296e0e", Name:="Basic Entitlement")>
    Public Function P012_InYearKS3_BESubtotal As Decimal
        Dim result = Decimal.Zero
        Dim acadfilter As Decimal = F100_AllAcademies
        Dim FundingBasis As String = Datasets.AcademyInformation.FundingBasis
        Dim P010_KS3_BESubtotal As Decimal = P010_KS3_BESubtotal
        Dim Days_Open As Decimal = P001_1718DaysOpen
        Dim Year_Days As Decimal = 365
        Print(P010_KS3_BESubtotal, "P010_KS3_BESubtotal", rid)
        Print(Days_Open, "Days Open", rid)
        Print(Year_Days, "Year Days", rid)
        If FundingBasis = "Place" Then
            exclude(rid)
        Else
            If acadfilter = 17181 Or acadfilter = 17182 Or acadfilter = 17183 Then
                Result =(P010_KS3_BESubtotal) * Days_Open / Year_Days
            Else
                exclude(rid)
            End If
        End If

        Return result
    End Function

    <Calculation(Id:="202e12c941024a32b2f8368fe484f58d")>
    <CalculationSpecification(Id:="P014_KS4Rate", Name:="P014_KS4Rate")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="bb411f3c107a40a8a71ff57604296e0e", Name:="Basic Entitlement")>
    Public Function P014_KS4Rate As Decimal
        Dim result = Decimal.Zero
        Dim acadfilter As Decimal = F100_AllAcademies
        Dim FundingBasis As String = Datasets.AcademyInformation.FundingBasis
        Dim P014_KS4Rate As Decimal = LAtoProv(Datasets.APTProformadataset.BasicEntitlementKS4AmountPerPupil)
        If FundingBasis = "Place" Then
            exclude(rid)
        Else
            If acadfilter = 17181 Or acadfilter = 17182 Or acadfilter = 17183 Then
                result = P014_KS4Rate
            Else
                Exclude(rid)
            End If
        End If

        Return result
    End Function

    <Calculation(Id:="c3d7248be3164d58af594dbc1b9b37dd")>
    <CalculationSpecification(Id:="P015_KS4_BESubtotal", Name:="P015_KS4_BESubtotal")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="bb411f3c107a40a8a71ff57604296e0e", Name:="Basic Entitlement")>
    Public Function P015_KS4_BESubtotal As Decimal
        Dim result = Decimal.Zero
        Dim F100_AllAcademies As Decimal = F100_AllAcademies
        Dim FundingBasis As String = Datasets.AcademyInformation.FundingBasis
        Dim P013_KS4Pupils As Decimal = NOR_P24_Total_NOR_KS4_SBS
        Dim P014_KS4Rate As Decimal = P014_KS4Rate
        Dim P015_KS4_BESubtotal As Decimal =(P013_KS4Pupils * P014_KS4Rate)
        Dim P015_KS4_BESubtotalAPT As Decimal = Datasets.APTNewISBdataset.BasicEntitlementKS4
        Print(F100_AllAcademies, "F100_AllAcademies", rid)
        Print(P013_KS4Pupils, "P013_KS4Pupils", rid)
        Print(P014_KS4Rate, "P014_KS4Rate", rid)
        Print(P015_KS4_BESubtotal, "P015_KS4_BESubtotal", rid)
        If FundingBasis = "Place" Then
            exclude(rid)
        Else
            If F100_AllAcademies = 17181 Or (F100_AllAcademies = 17182 And FundingBasis = "Estimate") Or (F100_AllAcademies = 17183 And FundingBasis = "Estimate") THEN
                Result = P015_KS4_BESubtotal
            Else
                If(F100_AllAcademies = 17182 And FundingBasis = "Census") Or (F100_AllAcademies = 17183 And FundingBasis = "Census") THEN
                    Result = P015_KS4_BESubtotalAPT
                Else
                    exclude(rid)
                End If
            End If
        End If

        Return result
    End Function

    <Calculation(Id:="7d2909411c13419c8ebb10ee19a8f2c4")>
    <CalculationSpecification(Id:="P016_NSEN_KS4BE", Name:="P016_NSEN_KS4BE")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="bb411f3c107a40a8a71ff57604296e0e", Name:="Basic Entitlement")>
    Public Function P016_NSEN_KS4BE As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim acadfilter As Decimal = F100_AllAcademies
        Dim FundingBasis As String = Datasets.AcademyInformation.FundingBasis
        Dim P015_KS4_BESubtotal As Decimal = P015_KS4_BESubtotal
        Dim P016a_NSEN_KS4BE_percent As Decimal = P016a_NSEN_KS4BE_percent
        Dim P016_NSEN_KS4BE As Decimal = P015_KS4_BESubtotal * P016a_NSEN_KS4BE_percent
        Print(P015_KS4_BESubtotal, "P015_KS4_BESubtotal", rid)
        Print(P016a_NSEN_KS4BE_percent, "P016a_NSEN_KS4BE_percent", rid)
        If FundingBasis = "Place" Then
            exclude(rid)
        Else
            If acadfilter = 17181 Or acadfilter = 17182 Or acadfilter = 17183 Then
                Result = P016_NSEN_KS4BE / 100
            Else
                Exclude(rid)
            End If
        End If

        Return result
    End Function

    <Calculation(Id:="faa31659578742dba28ce8ee8e4cc91f")>
    <CalculationSpecification(Id:="P016a_NSEN_KS4BE_Percent", Name:="P016a_NSEN_KS4BE_Percent")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="bb411f3c107a40a8a71ff57604296e0e", Name:="Basic Entitlement")>
    Public Function P016a_NSEN_KS4BE_Percent As Decimal
        Dim result = Decimal.Zero
        Dim F200_SBS_Academies As Decimal = F200_SBS_Academies
        Dim NSEN_KS4BE_Percent As Decimal = LAtoProv(Datasets.APTProformadataset.BasicEntitlementKS4NotionalSEN)
        Dim P016a_NSEN_KS4BE_Percent As Decimal = NSEN_KS4BE_Percent * 100
        If(F200_SBS_Academies = 1) Then
            Result = P016a_NSEN_KS4BE_Percent
        Else
            Exclude(rid)
        End if

        Print(F200_SBS_Academies, "F200_SBS_Academies", rid)
        Print(NSEN_KS4BE_Percent, "NSEN_KS4BE_Percent", rid)
        Print(P016a_NSEN_KS4BE_Percent, "P016a_NSEN_KS4BE_Percent", rid)
        Return result
    End Function

    <Calculation(Id:="f1974392fb6a410fa4a883ef6e8a315e")>
    <CalculationSpecification(Id:="P018_InYearKS4_BESubtotal", Name:="P018_InYearKS4_BESubtotal")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="bb411f3c107a40a8a71ff57604296e0e", Name:="Basic Entitlement")>
    Public Function P018_InYearKS4_BESubtotal As Decimal
        Dim result = Decimal.Zero
        Dim acadfilter As Decimal = F100_AllAcademies
        Dim FundingBasis As String = Datasets.AcademyInformation.FundingBasis
        Dim P015_KS4_BESubtotal As Decimal = P015_KS4_BESubtotal
        Dim Days_Open As Decimal = P001_1718DaysOpen
        Dim Year_Days As Decimal = 365
        Print(P015_KS4_BESubtotal, "P015_KS4_BESubtotal", rid)
        Print(Days_Open, "Days Open", rid)
        Print(Year_Days, "Year Days", rid)
        If FundingBasis = "Place" Then
            exclude(rid)
        Else
            If acadfilter = 17181 Or acadfilter = 17182 Or acadfilter = 17183 Then
                Result =(P015_KS4_BESubtotal) * Days_Open / Year_Days
            Else
                Exclude(rid)
            End If
        End If

        Return result
    End Function

    <Calculation(Id:="6a32253606db4c0bb88d052fd47e9775")>
    <CalculationSpecification(Id:="P297_DedelegationRetained", Name:="P297_DedelegationRetained")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="cc9eac91ede6480eb46f748efb3a9d11", Name:="Dedelegation Retained by LA")>
    Public Function P297_DedelegationRetained As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim F100_AllAcademies As Decimal = F100_AllAcademies
        Dim FundingBasis As String = Datasets.AcademyInformation.FundingBasis
        Dim F200_SBS_Academies As Decimal = F200_SBS_Academies
        Dim P295_Dedelegation As Decimal = P295_Dedelegation
        Dim P001_1718DaysOpen As Decimal = P001_1718DaysOpen
        Dim Year_Days As Decimal = 365
        If F200_SBS_Academies = 1 then
            If F100_AllAcademies = 17183 then
                IF P001_1718DaysOpen = 365 THEN
                    Result = 0
                ELSE
                    Result =(P001_1718DaysOpen - 153) / Year_Days * -P295_Dedelegation
                End If
            End If

            If(F100_AllAcademies = 17181) Or (F100_AllAcademies = 17182) then
                Result = 0
            End If
        ELSE
            Exclude(rid)
        End If

        Print(F100_AllAcademies, "F100_AllAcademies", rid)
        Print(FundingBasis, "FundingBasis", rid)
        Print(F200_SBS_Academies, "F200_SBS_Academies", rid)
        Print(P295_Dedelegation, "P295_Dedelegation", rid)
        Print(P001_1718DaysOpen, "P001_1718DaysOpen", rid)
        Print(Year_Days, "Year_Days", rid)
        Return result
    End Function

    <Calculation(Id:="0b610777b6464c2d908dd6a050521526")>
    <CalculationSpecification(Id:="P142_EAL1PriFactor", Name:="P142_EAL1PriFactor")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="dfd825e4a5784fa188379b080fa0c4ae", Name:="EAL")>
    Public Function P142_EAL1PriFactor As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim F100_AllAcademies As Decimal = F100_AllAcademies
        Dim FundingBasis As Decimal = F900_FundingBasis
        Dim F200_SBS_Academies As Decimal = F200_SBS_Academies
        Dim EALPrimP1PupilCharac_String As String = Datasets.CensusPupilCharacteristics.EAL1PrimaryProportion
        Dim EALPrimaryP1Adj_String As String = Datasets.APTInputsandAdjustments.EAL1PrimaryProportion
        Dim LAAV_EALPrimP1 As Decimal = LaToProv(Datasets.LocalAuthorityAverages.EAL1PrimaryProportion)
        Dim PupilCharacCensusEALP1 As Decimal = Datasets.CensusPupilCharacteristics.EAL1PrimaryProportion
        Dim AdjEALP1 As Decimal = Datasets.APTInputsandAdjustments.EAL1PrimaryProportion
        If F200_SBS_Academies = 1 then
            If string.IsNullOrEmpty(EALPrimP1PupilCharac_String) And String.IsNullOrEmpty(EALPrimaryP1Adj_String) then
                Result = LAAV_EALPrimP1
            Else
                If String.IsNullOrEmpty(EALPrimaryP1Adj_String) then
                    Result = PupilCharacCensusEALP1
                Else
                    Result = AdjEALP1
                End If
            End If
        Else
            exclude(rid)
        End If

        Print(F100_AllAcademies, "F100_AllAcademies", rid)
        Print(FundingBasis, "FundingBasis", rid)
        Print(F200_SBS_Academies, "F200_SBS_Academies", rid)
        Print(EALPrimP1PupilCharac_String, "EALPrimP1PupilCharac_String", rid)
        Print(EALPrimaryP1Adj_String, "EALPrimaryP1Adj_String", rid)
        Print(LAAV_EALPrimP1, "LAAV_EALPrimP1", rid)
        Print(PupilCharacCensusEALP1, "PupilCharacCensusEALP1", rid)
        Print(AdjEALP1, "AdjEALP1", rid)
        Return result
    End Function

    <Calculation(Id:="9c444ba706db449aa2f034656fb4866a")>
    <CalculationSpecification(Id:="P144_EAL1PriRate", Name:="P144_EAL1PriRate")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="dfd825e4a5784fa188379b080fa0c4ae", Name:="EAL")>
    Public Function P144_EAL1PriRate As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim F100_AllAcademies As Decimal = F100_AllAcademies
        Dim FundingBasis As Decimal = F900_FundingBasis
        Dim F200_SBS_Academies As Decimal = F200_SBS_Academies
        Dim EAL_Primary As String = LaToProv(Datasets.APTProformadataset.EALPrimary123NA)
        Dim EAL_Primary_Amount_Per_Pupil As Decimal = LaToProv(Datasets.APTProformadataset.EALPrimaryAmountPerPupil)
        If F200_SBS_Academies = 1 then
            If EAL_Primary = "EAL 1 Primary" Then
                Result = EAL_Primary_Amount_Per_Pupil
            Else
                Result = 0
            End IF
        Else
            exclude(rid)
        End If

        Print(F100_AllAcademies, "F100_AllAcademies", rid)
        Print(FundingBasis, "FundingBasis", rid)
        Print(F200_SBS_Academies, "F200_SBS_Academies", rid)
        Print(EAL_Primary, "EEAL_Primary_1/2/3/NA", rid)
        Print(EAL_Primary_Amount_Per_Pupil, "EAL_Primary_Amount_Per_Pupil", rid)
        Return result
    End Function

    <Calculation(Id:="e4cd10f9f4eb4449b9caf91cbfb13aec")>
    <CalculationSpecification(Id:="P145_EAL1PriSubtotal", Name:="P145_EAL1PriSubtotal")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="dfd825e4a5784fa188379b080fa0c4ae", Name:="EAL")>
    Public Function P145_EAL1PriSubtotal As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim F100_AllAcademies As Decimal = F100_AllAcademies
        Dim FundingBasis As Decimal = F900_FundingBasis
        Dim F200_SBS_Academies As Decimal = F200_SBS_Academies
        Dim P142_EAL1PriFactor As Decimal = P142_EAL1PriFactor
        Dim P144_EAL1PriRate As Decimal = P144_EAL1PriRate
        Dim P22_Total_NOR_PRI_SBS As Decimal = NOR_P22_Total_NOR_PRI_SBS
        Dim calc As Decimal = P22_Total_NOR_PRI_SBS * P144_EAL1PriRate * P142_EAL1PriFactor
        Dim EAL_P As Decimal = LAToProv(Datasets.APTNewISBdataset.EALP)
        If F200_SBS_Academies <> 1 Then
            exclude(rid)
        End if

        If(F100_AllAcademies = 17181) Or (FundingBasis = 2 And F100_AllAcademies = 17182) Or (FundingBasis = 2 And F100_AllAcademies = 17183) then
            result = calc
        Else
            If(FundingBasis = 1 And F100_AllAcademies = 17182) Or (FundingBasis = 1 And F100_AllAcademies = 17183) then 'new opener
                If P144_EAL1PriRate > 0
                    Result = EAL_P
                Else
                    Result = 0
                End If
            End If
        End If

        Print(F100_AllAcademies, "F100_AllAcademies", rid)
        Print(FundingBasis, "FundingBasis", rid)
        Print(F200_SBS_Academies, "F200_SBS_Academies", rid)
        Print(P142_EAL1PriFactor, "P142_EAL1PriFactor", rid)
        Print(P144_EAL1PriRate, "P144_EAL1PriRate", rid)
        Print(P22_Total_NOR_PRI_SBS, "P22_Total_NOR_PRI_SB", rid)
        Print(EAL_P, "EAL_P", rid)
        Print(calc, "calc", rid)
        Return result
    End Function

    <Calculation(Id:="e11834caaaea4929ba06edfe189bf83a")>
    <CalculationSpecification(Id:="P146_InYearEAL1PriSubtotal", Name:="P146_InYearEAL1PriSubtotal")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="dfd825e4a5784fa188379b080fa0c4ae", Name:="EAL")>
    Public Function P146_InYearEAL1PriSubtotal As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim F100_AllAcademies As Decimal = F100_AllAcademies
        Dim FundingBasis As Decimal = F900_FUndingBasis
        Dim F200_SBS_Academies As Decimal = F200_SBS_Academies
        Dim P145_EAL1PriSubtotal As Decimal = P145_EAL1PriSubtotal
        Dim P001_1718DaysOpen As Decimal = P001_1718DaysOpen
        Dim Year_Days As Decimal = 365
        If F200_SBS_Academies = 1 Then
            Result = P145_EAL1PriSubtotal * P001_1718DaysOpen / Year_Days
        Else
            Exclude(rid)
        End If

        Print(F100_AllAcademies, "F100_AllAcademies", rid)
        Print(FundingBasis, "FundingBasis", rid)
        Print(F200_SBS_Academies, "F200_SBS_Academies", rid)
        Print(P145_EAL1PriSubtotal, "P145_EAL1PriSubtotal", rid)
        Print(P001_1718DaysOpen, "P001_1617DaysOpen", rid)
        Print(Year_Days, "Year_Days", rid)
        Return result
    End Function

    <Calculation(Id:="5ec0e225bc494dd3abb8715e9c55469b")>
    <CalculationSpecification(Id:="P147_EAL2PriFactor", Name:="P147_EAL2PriFactor")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="dfd825e4a5784fa188379b080fa0c4ae", Name:="EAL")>
    Public Function P147_EAL2PriFactor As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim acadfilter As Decimal = F100_AllAcademies
        Dim FundingBasis As String = Datasets.AcademyInformation.FundingBasis
        Dim F200_SBS_Academies As Decimal = F200_SBS_Academies
        Dim EALPrimP2PupilCharac_String As String = Datasets.CensusPupilCharacteristics.EAL2PrimaryProportion
        Dim EALPrimaryP2Adj_String As String = Datasets.APTInputsandAdjustments.EAL2PrimaryProportion
        Dim LAAV_EALPrimP2 As Decimal = LaToProv(Datasets.LocalAuthorityAverages.EAL2PrimaryProportion)
        Dim PupilCharacCensusEALP2 As Decimal = Datasets.CensusPupilCharacteristics.EAL2PrimaryProportion
        Dim AdjEALP2 As Decimal = Datasets.APTInputsandAdjustments.EAL2PrimaryProportion
        If FundingBasis = "Place" Then
            exclude(rid)
        Else
            If acadfilter = 17181 Or acadfilter = 17182 Or acadfilter = 17183 Then
                If string.IsNullOrEmpty(EALPrimP2PupilCharac_String) And String.IsNullOrEmpty(EALPrimaryP2Adj_String) Then
                    Result = LAAV_EALPrimP2
                Else
                    If String.IsNullOrEmpty(EALPrimaryP2Adj_String) Then
                        Result = PupilCharacCensusEALP2
                    Else
                        Result = AdjEALP2
                    End If
                End If
            Else
                exclude(rid)
            End If
        End If

        Print(FundingBasis, "FundingBasis", rid)
        Print(F200_SBS_Academies, "F200_SBS_Academies", rid)
        Print(EALPrimP2PupilCharac_String, "EALPrimP2PupilCharac_String", rid)
        Print(EALPrimaryP2Adj_String, "EALPrimaryP2Adj_String", rid)
        Print(LAAV_EALPrimP2, "LAAV_EALPrimP2", rid)
        Print(PupilCharacCensusEALP2, "PupilCharacCensusEALP2", rid)
        Print(AdjEALP2, "AdjEALP2", rid)
        Return result
    End Function

    <Calculation(Id:="37fae1af9ff04ab090cebd8ab7910d10")>
    <CalculationSpecification(Id:="P149_EAL2PriRate", Name:="P149_EAL2PriRate")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="dfd825e4a5784fa188379b080fa0c4ae", Name:="EAL")>
    Public Function P149_EAL2PriRate As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim F100_AllAcademies As Decimal = F100_AllAcademies
        Dim FundingBasis As Decimal = F900_FundingBasis
        Dim F200_SBS_Academies As Decimal = F200_SBS_Academies
        Dim EAL_Primary As String = LaToProv(Datasets.APTProformadataset.EALPrimary123NA)
        Dim EAL_Primary_Amount_Per_Pupil As Decimal = LaToProv(Datasets.APTProformadataset.EALPrimaryAmountPerPupil)
        If F200_SBS_Academies = 1 then
            If EAL_Primary = "EAL 2 Primary" Then
                Result = EAL_Primary_Amount_Per_Pupil
            Else
                Result = 0
            End IF
        Else
            exclude(rid)
        End If

        Print(F100_AllAcademies, "F100_AllAcademies", rid)
        Print(FundingBasis, "FundingBasis", rid)
        Print(F200_SBS_Academies, "F200_SBS_Academies", rid)
        Print(EAL_Primary, "EEAL_Primary_1/2/3/NA", rid)
        Print(EAL_Primary_Amount_Per_Pupil, "EAL_Primary_Amount_Per_Pupil", rid)
        Return result
    End Function

    <Calculation(Id:="a60d657f7dbb4c43914d2fa9bc34c5c1")>
    <CalculationSpecification(Id:="P150_EAL2PriSubtotal", Name:="P150_EAL2PriSubtotal")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="dfd825e4a5784fa188379b080fa0c4ae", Name:="EAL")>
    Public Function P150_EAL2PriSubtotal As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim F100_AllAcademies As Decimal = F100_AllAcademies
        Dim FundingBasis As String = Datasets.AcademyInformation.FundingBasis
        Dim F200_SBS_Academies As Decimal = F200_SBS_Academies
        Dim P149_EAL2PriRate As Decimal = P149_EAL2PriRate
        Dim P147_EAL2PriFactor As Decimal = P147_EAL2PriFactor
        Dim P22_Total_NOR_PRI_SBS As Decimal = NOR_P22_Total_NOR_PRI_SBS
        Dim calc As Decimal = P22_Total_NOR_PRI_SBS * P149_EAL2PriRate * P147_EAL2PriFactor
        Dim EAL_P As Decimal = LAToProv(Datasets.APTNewISBdataset.EALP)
        If FundingBasis = "Place" Then
            exclude(rid)
        Else
            If(F100_AllAcademies = 17181) Or (FundingBasis = "Estimate" And F100_AllAcademies = 17182) Or (FundingBasis = "Estimate" And F100_AllAcademies = 17183) Then
                result = calc
            Else
                If(FundingBasis = "Census" And F100_AllAcademies = 17182) Or (FundingBasis = "Census" And F100_AllAcademies = 17183) Then 'new opener
                    If P149_EAL2PriRate > 0
                        Result = EAL_P
                    Else
                        Result = 0
                    End If
                Else
                    exclude(rid)
                End If
            End If
        End If

        Return result
    End Function

    <Calculation(Id:="db33cc22084b45cbb96321c464379b81")>
    <CalculationSpecification(Id:="P151_InYearEAL2PriSubtotal", Name:="P151_InYearEAL2PriSubtotal")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="dfd825e4a5784fa188379b080fa0c4ae", Name:="EAL")>
    Public Function P151_InYearEAL2PriSubtotal As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim F100_AllAcademies As Decimal = F100_AllAcademies
        Dim FundingBasis As Decimal = F900_FundingBasis
        Dim F200_SBS_Academies As Decimal = F200_SBS_Academies
        Dim P150_EAL2PriSubtotal As Decimal = P150_EAL2PriSubtotal
        Dim P001_1718DaysOpen As Decimal = P001_1718DaysOpen
        Dim Year_Days As Decimal = 365
        If F200_SBS_Academies = 1 then
            Result = P150_EAL2PriSubtotal * P001_1718DaysOpen / Year_Days
        ELSE
            Exclude(rid)
        End IF

        Print(F100_AllAcademies, "F100_AllAcademies", rid)
        Print(FundingBasis, "FundingBasis", rid)
        Print(F200_SBS_Academies, "F200_SBS_Academies", rid)
        Print(P150_EAL2PriSubtotal, "P150_EAL2PriSubtotal", rid)
        Print(P001_1718DaysOpen, "P001_1718DaysOpen", rid)
        Print(Year_Days, "Year_Days", rid)
        Return result
    End Function

    <Calculation(Id:="035d60ec3bed404bb072f6c27baf91a3")>
    <CalculationSpecification(Id:="P152_EAL3PriFactor", Name:="P152_EAL3PriFactor")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="dfd825e4a5784fa188379b080fa0c4ae", Name:="EAL")>
    Public Function P152_EAL3PriFactor As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim F100_AllAcademies As Decimal = F100_AllAcademies
        Dim FundingBasis As decimal = F900_FundingBasis
        Dim F200_SBS_Academies As Decimal = F200_SBS_Academies
        Dim EALPrimP3PupilCharac_String As String = Datasets.CensusPupilCharacteristics.EAL3PrimaryProportion
        Dim EALPrimaryP3Adj_String As String = Datasets.APTInputsandAdjustments.EAL3PrimaryProportion
        Dim LAAV_EALPrimP3 As Decimal = LaToProv(Datasets.LocalAuthorityAverages.EAL3PrimaryProportion)
        Dim PupilCharacCensusEALP3 As Decimal = Datasets.CensusPupilCharacteristics.EAL3PrimaryProportion
        Dim AdjEALP3 As Decimal = Datasets.APTInputsandAdjustments.EAL3PrimaryProportion
        If F200_SBS_Academies = 1 then
            If string.IsNullOrEmpty(EALPrimP3PupilCharac_String) And String.IsNullOrEmpty(EALPrimaryP3Adj_String) then
                Result = LAAV_EALPrimP3
            Else
                If String.IsNullOrEmpty(EALPrimaryP3Adj_String) then
                    Result = PupilCharacCensusEALP3
                Else
                    Result = AdjEALP3
                End If
            End If
        Else
            exclude(rid)
        End If

        Print(F100_AllAcademies, "F100_AllAcademies", rid)
        Print(FundingBasis, "FundingBasis", rid)
        Print(F200_SBS_Academies, "F200_SBS_Academies", rid)
        Print(EALPrimP3PupilCharac_String, "EALPrimP3PupilCharac_String", rid)
        Print(EALPrimaryP3Adj_String, "EALPrimaryP3Adj_String", rid)
        Print(LAAV_EALPrimP3, "LAAV_EALPrimP3", rid)
        Print(PupilCharacCensusEALP3, "PupilCharacCensusEALP3", rid)
        Print(AdjEALP3, "AdjEALP3", rid)
        Return result
    End Function

    <Calculation(Id:="94ebb1d013614ed7b1677d6f9ca35e5e")>
    <CalculationSpecification(Id:="P154_EAL3PriRate", Name:="P154_EAL3PriRate")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="dfd825e4a5784fa188379b080fa0c4ae", Name:="EAL")>
    Public Function P154_EAL3PriRate As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim F100_AllAcademies As Decimal = F100_AllAcademies
        Dim FundingBasis As Decimal = F900_FundingBasis
        Dim F200_SBS_Academies As Decimal = F200_SBS_Academies
        Dim EAL_Primary As String = LaToProv(Datasets.APTProformadataset.EALPrimary123NA)
        Dim EAL_Primary_Amount_Per_Pupil As Decimal = LaToProv(Datasets.APTProformadataset.EALPrimaryAmountPerPupil)
        If F200_SBS_Academies = 1 then
            If EAL_Primary = "EAL 3 Primary" Then
                Result = EAL_Primary_Amount_Per_Pupil
            Else
                Result = 0
            End IF
        Else
            exclude(rid)
        End If

        Print(F100_AllAcademies, "F100_AllAcademies", rid)
        Print(FundingBasis, "FundingBasis", rid)
        Print(F200_SBS_Academies, "F200_SBS_Academies", rid)
        Print(EAL_Primary, "EEAL_Primary_1/2/3/NA", rid)
        Print(EAL_Primary_Amount_Per_Pupil, "EAL_Primary_Amount_Per_Pupil", rid)
        Return result
    End Function

    <Calculation(Id:="db9a5e96b34d494e88a5cf2ebc8b268f")>
    <CalculationSpecification(Id:="P155_EAL3PriSubtotal", Name:="P155_EAL3PriSubtotal")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="dfd825e4a5784fa188379b080fa0c4ae", Name:="EAL")>
    Public Function P155_EAL3PriSubtotal As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim F100_AllAcademies As Decimal = F100_AllAcademies
        Dim FundingBasis As String = Datasets.AcademyInformation.FundingBasis
        Dim P154_EAL3PriRate As Decimal = P154_EAL3PriRate
        Dim P152_EAL3PriFactor As Decimal = P152_EAL3PriFactor
        Dim P22_Total_NOR_PRI_SBS As Decimal = NOR_P22_Total_NOR_PRI_SBS
        Dim calc As Decimal = P22_Total_NOR_PRI_SBS * P154_EAL3PriRate * P152_EAL3PriFactor
        Dim EAL_P As Decimal = LAtoProv(Datasets.APTNewISBdataset.EALP)
        If FundingBasis = "Place" Then
            exclude(rid)
        Else
            If(F100_AllAcademies = 17181) Or (FundingBasis = "Estimate" And F100_AllAcademies = 17182) Or (FundingBasis = "Estimate" And F100_AllAcademies = 17183) Then
                result = calc
            Else
                If(FundingBasis = "Census" And F100_AllAcademies = 17182) Or (FundingBasis = "Census" And F100_AllAcademies = 17183) Then 'new opener
                    If P154_EAL3PriRate > 0
                        Result = EAL_P
                    Else
                        Result = 0
                    End If
                Else
                    exclude(rid)
                End If
            End If
        End If

        Return result
    End Function

    <Calculation(Id:="9ae77b8916474c9d9a707e7830583935")>
    <CalculationSpecification(Id:="P156_NSENPriEAL", Name:="P156_NSENPriEAL")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="dfd825e4a5784fa188379b080fa0c4ae", Name:="EAL")>
    Public Function P156_NSENPriEAL As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim F100_AllAcademies As Decimal = F100_AllAcademies
        Dim FundingBasis As Decimal = F900_FundingBasis
        Dim F200_SBS_Academies As Decimal = F200_SBS_Academies
        Dim P145_EAL1PriSubtotal As Decimal = P145_EAL1PriSubtotal
        Dim P150_EAL2PriSubtotal As Decimal = P150_EAL2PriSubtotal
        Dim P155_EAL3PriSubtotal As Decimal = P155_EAL3PriSubtotal
        Dim P156a_NSENPriEAL_Percent As Decimal = P156a_NSENPriEAL_Percent / 100
        If F200_SBS_Academies = 1 then
            Result =(P145_EAL1PriSubtotal + P150_EAL2PriSubtotal + P155_EAL3PriSubtotal) * P156a_NSENPriEAL_Percent
        Else
            exclude(rid)
        End If

        Print(F100_AllAcademies, "F100_AllAcademies", rid)
        Print(FundingBasis, "FundingBasis", rid)
        Print(F200_SBS_Academies, "F200_SBS_Academies", rid)
        Print(P145_EAL1PriSubtotal, "P145_EAL1PriSubtotal", rid)
        Print(P150_EAL2PriSubtotal, "P150_EAL2PriSubtotal", rid)
        Print(P155_EAL3PriSubtotal, "P155_EAL3PriSubtotal", rid)
        Print(P156a_NSENPriEAL_Percent, "P156a_NSENPriEAL_Percent", rid)
        Return result
    End Function

    <Calculation(Id:="15cb5d4bcf46439785f0c764dfdf7a02")>
    <CalculationSpecification(Id:="P156a_NSENPriEAL_Percent", Name:="P156a_NSENPriEAL_Percent")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="dfd825e4a5784fa188379b080fa0c4ae", Name:="EAL")>
    Public Function P156a_NSENPriEAL_Percent As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim F100_AllAcademies As Decimal = F100_AllAcademies
        Dim FundingBasis As Decimal = F900_FundingBasis
        Dim F200_SBS_Academies As Decimal = F200_SBS_Academies
        Dim EAL_Primary_Notional_SEN As Decimal = LaToProv(Datasets.APTProformadataset.EALPrimaryNotionalSEN)
        If F200_SBS_Academies = 1 then
            Result = EAL_Primary_Notional_SEN * 100
        Else
            exclude(rid)
        End If

        Print(F100_AllAcademies, "F100_AllAcademies", rid)
        Print(FundingBasis, "FundingBasis", rid)
        Print(F200_SBS_Academies, "F200_SBS_Academies", rid)
        Print(EAL_Primary_Notional_SEN, "EAL_Primary_Notional_SEN", rid)
        Return result
    End Function

    <Calculation(Id:="0a7b33b3a3644334a7b1968028bbec67")>
    <CalculationSpecification(Id:="P157_InYearEAL3PriSubtotal", Name:="P157_InYearEAL3PriSubtotal")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="dfd825e4a5784fa188379b080fa0c4ae", Name:="EAL")>
    Public Function P157_InYearEAL3PriSubtotal As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim F100_AllAcademies As Decimal = F100_AllAcademies
        Dim FundingBasis As Decimal = F900_FundingBasis
        Dim F200_SBS_Academies As Decimal = F200_SBS_Academies
        Dim P155_EAL3PriSubtotal As Decimal = P155_EAL3PriSubtotal
        Dim P001_1718DaysOpen As Decimal = P001_1718DaysOpen
        Dim Year_Days As Decimal = 365
        If F200_SBS_Academies = 1 then
            Result = P155_EAL3PriSubtotal * P001_1718DaysOpen / Year_Days
        ELSE
            Exclude(rid)
        End IF

        Print(F100_AllAcademies, "F100_AllAcademies", rid)
        Print(FundingBasis, "FundingBasis", rid)
        Print(F200_SBS_Academies, "F200_SBS_Academies", rid)
        Print(P155_EAL3PriSubtotal, "P155_EAL3PriSubtotal", rid)
        Print(P001_1718DaysOpen, "P001_1718DaysOpen", rid)
        Print(Year_Days, "Year_Days", rid)
        Return result
    End Function

    <Calculation(Id:="7e8f5ab9ce034212bfc55086c9cd0bcd")>
    <CalculationSpecification(Id:="P158_EAL1SecFactor", Name:="P158_EAL1SecFactor")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="dfd825e4a5784fa188379b080fa0c4ae", Name:="EAL")>
    Public Function P158_EAL1SecFactor As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim F100_AllAcademies As Decimal = F100_AllAcademies
        Dim FundingBasis As Decimal = F900_FundingBasis
        Dim F200_SBS_Academies As Decimal = F200_SBS_Academies
        Dim EALSecP1PupilCharac_String As String = Datasets.CensusPupilCharacteristics.EAL1SecondaryProportion
        Dim EALSecondaryP1Adj_String As String = Datasets.APTInputsandAdjustments.EAL1SecondaryProportion
        Dim LAAV_EALSecP1 As Decimal = LaToProv(Datasets.LocalAuthorityAverages.EAL1SecondaryProportion)
        Dim PupilCharacCensusEALSecP1 As Decimal = Datasets.CensusPupilCharacteristics.EAL1SecondaryProportion
        Dim AdjEALSecP1 As Decimal = Datasets.APTInputsandAdjustments.EAL1SecondaryProportion
        If F200_SBS_Academies = 1 then
            If string.IsNullOrEmpty(EALSecP1PupilCharac_String) And String.IsNullOrEmpty(EALSecondaryP1Adj_String) then
                Result = LAAV_EALSecP1
            Else
                If String.IsNullOrEmpty(EALSecondaryP1Adj_String) then
                    Result = PupilCharacCensusEALSecP1
                Else
                    Result = AdjEALSecP1
                End If
            End If
        Else
            exclude(rid)
        End If

        Print(F100_AllAcademies, "F100_AllAcademies", rid)
        Print(FundingBasis, "FundingBasis", rid)
        Print(F200_SBS_Academies, "F200_SBS_Academies", rid)
        Print(EALSecP1PupilCharac_String, "EALSecP1PupilCharac_String", rid)
        Print(EALSecondaryP1Adj_String, "EALSecondaryP1Adj_String", rid)
        Print(LAAV_EALSecP1, "LAAV_EALSecP1", rid)
        Print(PupilCharacCensusEALSecP1, "PupilCharacCensusEALSecP1", rid)
        Print(AdjEALSecP1, "AdjEALSecP1", rid)
        Return result
    End Function

    <Calculation(Id:="9dd20892fed3402893a1aca883a60b1c")>
    <CalculationSpecification(Id:="P160_EAL1SecRate", Name:="P160_EAL1SecRate")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="dfd825e4a5784fa188379b080fa0c4ae", Name:="EAL")>
    Public Function P160_EAL1SecRate As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim F100_AllAcademies As Decimal = F100_AllAcademies
        Dim FundingBasis As Decimal = F900_FundingBasis
        Dim F200_SBS_Academies As Decimal = F200_SBS_Academies
        Dim EAL_Secondary As String = LaToProv(Datasets.APTProformadataset.EALSecondary123NA)
        Dim EAL_Secondary_Amount_Per_Pupil As Decimal = LaToProv(Datasets.APTProformadataset.EALSecondaryAmountPerPupil)
        If F200_SBS_Academies = 1 then
            If EAL_Secondary = "EAL 1 Secondary" Then
                Result = EAL_Secondary_Amount_Per_Pupil
            Else
                Result = 0
            End IF
        Else
            exclude(rid)
        End If

        Print(F100_AllAcademies, "F100_AllAcademies", rid)
        Print(FundingBasis, "FundingBasis", rid)
        Print(F200_SBS_Academies, "F200_SBS_Academies", rid)
        Print(EAL_Secondary, "EEAL_Secondary_1/2/3/NA", rid)
        Print(EAL_Secondary_Amount_Per_Pupil, "EAL_Secondary_Amount_Per_Pupil", rid)
        Return result
    End Function

    <Calculation(Id:="92a66eed34e043c995f96f9742f7322b")>
    <CalculationSpecification(Id:="P161_EAL1SecSubtotal", Name:="P161_EAL1SecSubtotal")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="dfd825e4a5784fa188379b080fa0c4ae", Name:="EAL")>
    Public Function P161_EAL1SecSubtotal As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim F100_AllAcademies As Decimal = F100_AllAcademies
        Dim FundingBasis As Decimal = F900_FundingBasis
        Dim F200_SBS_Academies As Decimal = F200_SBS_Academies
        Dim P160_EAL1SecRate As Decimal = P160_EAL1SecRate
        Dim P158_EAL1SecFactor As Decimal = P158_EAL1SecFactor
        Dim P25_Total_NOR_SEC_SBS As Decimal = NOR_P25_Total_NOR_SEC_SBS
        Dim calc As Decimal = P25_Total_NOR_SEC_SBS * P160_EAL1SecRate * P158_EAL1SecFactor
        Dim EAL_S As Decimal = LAToProv(Datasets.APTNewISBdataset.EALS)
        If F200_SBS_Academies <> 1 Then
            exclude(rid)
        End if

        If(F100_AllAcademies = 17181) Or (FundingBasis = 2 And F100_AllAcademies = 17182) Or (FundingBasis = 2 And F100_AllAcademies = 17183) then
            result = calc
        Else
            If(FundingBasis = 1 And F100_AllAcademies = 17182) Or (FundingBasis = 1 And F100_AllAcademies = 17183) then 'new opener
                If P160_EAL1SecRate > 0
                    Result = EAL_S
                Else
                    Result = 0
                End If
            End If
        End If

        Print(F100_AllAcademies, "F100_AllAcademies", rid)
        Print(FundingBasis, "FundingBasis", rid)
        Print(F200_SBS_Academies, "F200_SBS_Academies", rid)
        Print(P160_EAL1SecRate, "P160_EAL1SecRate", rid)
        Print(P158_EAL1SecFactor, "P158_EAL1SecFactor", rid)
        Print(P25_Total_NOR_SEC_SBS, "P25_Total_NOR_SEC_SBS", rid)
        Print(EAL_S, "EAL_S", rid)
        Print(calc, "calc", rid)
        Return result
    End Function

    <Calculation(Id:="5dbb6f2fc1704471a203bb3494e2bd3d")>
    <CalculationSpecification(Id:="P162_InYearEAL1SecSubtotal", Name:="P162_InYearEAL1SecSubtotal")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="dfd825e4a5784fa188379b080fa0c4ae", Name:="EAL")>
    Public Function P162_InYearEAL1SecSubtotal As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim F100_AllAcademies As Decimal = F100_AllAcademies
        Dim FundingBasis As Decimal = F900_FundingBasis
        Dim F200_SBS_Academies As Decimal = F200_SBS_Academies
        Dim P161_EAL1SecSubtotal As Decimal = P161_EAL1SecSubtotal
        Dim P001_1718DaysOpen As Decimal = P001_1718DaysOpen
        Dim Year_Days As Decimal = 365
        If F200_SBS_Academies = 1 then
            Result = P161_EAL1SecSubtotal * P001_1718DaysOpen / Year_Days
        ELSE
            Exclude(rid)
        End IF

        Print(F100_AllAcademies, "F100_AllAcademies", rid)
        Print(FundingBasis, "FundingBasis", rid)
        Print(F200_SBS_Academies, "F200_SBS_Academies", rid)
        Print(P161_EAL1SecSubtotal, "P161_EAL1SecSubtotal", rid)
        Print(P001_1718DaysOpen, "P001_1718DaysOpen", rid)
        Print(Year_Days, "Year_Days", rid)
        Return result
    End Function

    <Calculation(Id:="536bac6e23b54298ba0acb2c6255648b")>
    <CalculationSpecification(Id:="P163_EAL2SecFactor", Name:="P163_EAL2SecFactor")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="dfd825e4a5784fa188379b080fa0c4ae", Name:="EAL")>
    Public Function P163_EAL2SecFactor As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim F100_AllAcademies As Decimal = F100_AllAcademies
        Dim FundingBasis As Decimal = F900_FundingBasis
        Dim F200_SBS_Academies As Decimal = F200_SBS_Academies
        Dim EALSecP2PupilCharac_String As String = Datasets.CensusPupilCharacteristics.EAL2SecondaryProportion
        Dim EALSecondaryP2Adj_String As String = Datasets.APTInputsandAdjustments.EAL2SecondaryProportion
        Dim LAAV_EALSecP2 As Decimal = LaToProv(Datasets.LocalAuthorityAverages.EAL2SecondaryProportion)
        Dim PupilCharacCensusEALSecP2 As Decimal = Datasets.CensusPupilCharacteristics.EAL2SecondaryProportion
        Dim AdjEALSecP2 As Decimal = Datasets.APTInputsandAdjustments.EAL2SecondaryProportion
        If F200_SBS_Academies = 1 then
            If string.IsNullOrEmpty(EALSecP2PupilCharac_String) And String.IsNullOrEmpty(EALSecondaryP2Adj_String) then
                Result = LAAV_EALSecP2
            Else
                If String.IsNullOrEmpty(EALSecondaryP2Adj_String) then
                    Result = PupilCharacCensusEALSecP2
                Else
                    Result = AdjEALSecP2
                End If
            End If
        Else
            exclude(rid)
        End If

        Print(F100_AllAcademies, "F100_AllAcademies", rid)
        Print(FundingBasis, "FundingBasis", rid)
        Print(F200_SBS_Academies, "F200_SBS_Academies", rid)
        Print(EALSecP2PupilCharac_String, "EALSecP2PupilCharac_String", rid)
        Print(EALSecondaryP2Adj_String, "EALSecondaryP2Adj_String", rid)
        Print(LAAV_EALSecP2, "LAAV_EALSecP2", rid)
        Print(PupilCharacCensusEALSecP2, "PupilCharacCensusEALSecP2", rid)
        Print(AdjEALSecP2, "AdjEALSecP2", rid)
        Return result
    End Function

    <Calculation(Id:="a487e5f8681e40038b6f6540b84c1284")>
    <CalculationSpecification(Id:="P165_EAL2SecRate", Name:="P165_EAL2SecRate")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="dfd825e4a5784fa188379b080fa0c4ae", Name:="EAL")>
    Public Function P165_EAL2SecRate As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim F100_AllAcademies As Decimal = F100_AllAcademies
        Dim FundingBasis As Decimal = F900_FundingBasis
        Dim F200_SBS_Academies As Decimal = F200_SBS_Academies
        Dim EAL_Secondary As String = LaToProv(Datasets.APTProformadataset.EALSecondary123NA)
        Dim EAL_Secondary_Amount_Per_Pupil As Decimal = LaToProv(Datasets.APTProformadataset.EALSecondaryAmountPerPupil)
        If F200_SBS_Academies = 1 then
            If EAL_Secondary = "EAL 2 Secondary" Then
                Result = EAL_Secondary_Amount_Per_Pupil
            Else
                Result = 0
            End IF
        Else
            exclude(rid)
        End If

        Print(F100_AllAcademies, "F100_AllAcademies", rid)
        Print(FundingBasis, "FundingBasis", rid)
        Print(F200_SBS_Academies, "F200_SBS_Academies", rid)
        Print(EAL_Secondary, "EEAL_Secondary_1/2/3/NA", rid)
        Print(EAL_Secondary_Amount_Per_Pupil, "EAL_Secondary_Amount_Per_Pupil", rid)
        Return result
    End Function

    <Calculation(Id:="56f7642445f442dbaea1413b0e6ec873")>
    <CalculationSpecification(Id:="P166_EAL2SecSubtotal", Name:="P166_EAL2SecSubtotal")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="dfd825e4a5784fa188379b080fa0c4ae", Name:="EAL")>
    Public Function P166_EAL2SecSubtotal As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim F100_AllAcademies As Decimal = F100_AllAcademies
        Dim FundingBasis As Decimal = F900_FundingBasis
        Dim F200_SBS_Academies As Decimal = F200_SBS_Academies
        Dim P165_EAL2SecRate As Decimal = P165_EAL2SecRate
        Dim P163_EAL2SecFactor As Decimal = P163_EAL2SecFactor
        Dim P25_Total_NOR_SEC_SBS As Decimal = NOR_P25_Total_NOR_SEC_SBS
        Dim calc As Decimal = P25_Total_NOR_SEC_SBS * P165_EAL2SecRate * P163_EAL2SecFactor
        Dim EAL_S As Decimal = LAToProv(Datasets.APTNewISBdataset.EALS)
        If F200_SBS_Academies <> 1 Then
            exclude(rid)
        End if

        If(F100_AllAcademies = 17181) Or (FundingBasis = 2 And F100_AllAcademies = 17182) Or (FundingBasis = 2 And F100_AllAcademies = 17183) then
            result = calc
        Else
            If(FundingBasis = 1 And F100_AllAcademies = 17182) Or (FundingBasis = 1 And F100_AllAcademies = 17183) then 'new opener
                If P165_EAL2SecRate > 0
                    Result = EAL_S
                Else
                    Result = 0
                End If
            End If
        End If

        Print(F100_AllAcademies, "F100_AllAcademies", rid)
        Print(FundingBasis, "FundingBasis", rid)
        Print(F200_SBS_Academies, "F200_SBS_Academies", rid)
        Print(P165_EAL2SecRate, "P165_EAL2SecRate", rid)
        Print(P163_EAL2SecFactor, "P163_EAL2SecFactor", rid)
        Print(P25_Total_NOR_SEC_SBS, "P25_Total_NOR_SEC_SBS", rid)
        Print(EAL_S, "EAL_S", rid)
        Print(calc, "calc", rid)
        Return result
    End Function

    <Calculation(Id:="0cefe103451140a3b3257f010feefbcf")>
    <CalculationSpecification(Id:="P167_InYearEAL2SecSubtotal", Name:="P167_InYearEAL2SecSubtotal")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="dfd825e4a5784fa188379b080fa0c4ae", Name:="EAL")>
    Public Function P167_InYearEAL2SecSubtotal As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim F100_AllAcademies As Decimal = F100_AllAcademies
        Dim FundingBasis As Decimal = F900_FundingBasis
        Dim F200_SBS_Academies As Decimal = F200_SBS_Academies
        Dim P166_EAL2SecSubtotal As Decimal = P166_EAL2SecSubtotal
        Dim P001_1718DaysOpen As Decimal = P001_1718DaysOpen
        Dim Year_Days As Decimal = 365
        If F200_SBS_Academies = 1 then
            Result = P166_EAL2SecSubtotal * P001_1718DaysOpen / Year_Days
        ELSE
            Exclude(rid)
        End IF

        Print(F100_AllAcademies, "F100_AllAcademies", rid)
        Print(FundingBasis, "FundingBasis", rid)
        Print(F200_SBS_Academies, "F200_SBS_Academies", rid)
        Print(P166_EAL2SecSubtotal, "P166_EAL2SecSubtotal", rid)
        Print(P001_1718DaysOpen, "P001_1718DaysOpen", rid)
        Print(Year_Days, "Year_Days", rid)
        Return result
    End Function

    <Calculation(Id:="2559c0232e94461d9be2c9de477c33ef")>
    <CalculationSpecification(Id:="P168_EAL3SecFactor", Name:="P168_EAL3SecFactor")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="dfd825e4a5784fa188379b080fa0c4ae", Name:="EAL")>
    Public Function P168_EAL3SecFactor As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim F100_AllAcademies As Decimal = F100_AllAcademies
        Dim FundingBasis As Decimal = F900_FundingBasis
        Dim F200_SBS_Academies As Decimal = F200_SBS_Academies
        Dim EALSecP3PupilCharac_String As String = Datasets.CensusPupilCharacteristics.EAL3SecondaryProportion
        Dim EALSecondaryP3Adj_String As String = Datasets.APTInputsandAdjustments.EAL3SecondaryProportion
        Dim LAAV_EALSecP3 As Decimal = LaToProv(Datasets.LocalAuthorityAverages.EAL3SecondaryProportion)
        Dim PupilCharacCensusEALSecP3 As Decimal = Datasets.CensusPupilCharacteristics.EAL3SecondaryProportion
        Dim AdjEALSecP3 As Decimal = Datasets.APTInputsandAdjustments.EAL3SecondaryProportion
        If F200_SBS_Academies = 1 then
            If string.IsNullOrEmpty(EALSecP3PupilCharac_String) And String.IsNullOrEmpty(EALSecondaryP3Adj_String) then
                Result = LAAV_EALSecP3
            Else
                If String.IsNullOrEmpty(EALSecondaryP3Adj_String) then
                    Result = PupilCharacCensusEALSecP3
                Else
                    Result = AdjEALSecP3
                End If
            End If
        Else
            exclude(rid)
        End If

        Print(F100_AllAcademies, "F100_AllAcademies", rid)
        Print(FundingBasis, "FundingBasis", rid)
        Print(F200_SBS_Academies, "F200_SBS_Academies", rid)
        Print(EALSecP3PupilCharac_String, "EALSecP3PupilCharac_String", rid)
        Print(EALSecondaryP3Adj_String, "EALSecondaryP3Adj_String", rid)
        Print(LAAV_EALSecP3, "LAAV_EALSecP3", rid)
        Print(PupilCharacCensusEALSecP3, "PupilCharacCensusEALSecP3", rid)
        Print(AdjEALSecP3, "AdjEALSecP3", rid)
        Return result
    End Function

    <Calculation(Id:="2b150ebbe2f94273bbaf75388b4a9200")>
    <CalculationSpecification(Id:="P170_EAL3SecRate", Name:="P170_EAL3SecRate")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="dfd825e4a5784fa188379b080fa0c4ae", Name:="EAL")>
    Public Function P170_EAL3SecRate As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim F100_AllAcademies As Decimal = F100_AllAcademies
        Dim FundingBasis As Decimal = F900_FundingBasis
        Dim F200_SBS_Academies As Decimal = F200_SBS_Academies
        Dim EAL_Secondary As String = LaToProv(Datasets.APTProformadataset.EALSecondary123NA)
        Dim EAL_Secondary_Amount_Per_Pupil As Decimal = LaToProv(Datasets.APTProformadataset.EALSecondaryAmountPerPupil)
        If F200_SBS_Academies = 1 then
            If EAL_Secondary = "EAL 3 Secondary" Then
                Result = EAL_Secondary_Amount_Per_Pupil
            Else
                Result = 0
            End IF
        Else
            exclude(rid)
        End If

        Print(F100_AllAcademies, "F100_AllAcademies", rid)
        Print(FundingBasis, "FundingBasis", rid)
        Print(F200_SBS_Academies, "F200_SBS_Academies", rid)
        Print(EAL_Secondary, "EEAL_Secondary_1/2/3/NA", rid)
        Print(EAL_Secondary_Amount_Per_Pupil, "EAL_Secondary_Amount_Per_Pupil", rid)
        Return result
    End Function

    <Calculation(Id:="5135c14ddc414dffbf9ab5fdf3f23916")>
    <CalculationSpecification(Id:="P171_EAL3SecSubtotal", Name:="P171_EAL3SecSubtotal")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="dfd825e4a5784fa188379b080fa0c4ae", Name:="EAL")>
    Public Function P171_EAL3SecSubtotal As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim F100_AllAcademies As Decimal = F100_AllAcademies
        Dim FundingBasis As Decimal = F900_FundingBasis
        Dim F200_SBS_Academies As Decimal = F200_SBS_Academies
        Dim P170_EAL3SecRate As Decimal = P170_EAL3SecRate
        Dim P168_EAL3SecFactor As Decimal = P168_EAL3SecFactor
        Dim P25_Total_NOR_SEC_SBS As Decimal = NOR_P25_Total_NOR_SEC_SBS
        Dim calc As Decimal = P25_Total_NOR_SEC_SBS * P170_EAL3SecRate * P168_EAL3SecFactor
        Dim EAL_S As Decimal = LAToProv(Datasets.APTNewISBdataset.EALS)
        If F200_SBS_Academies <> 1 Then
            exclude(rid)
        End if

        If(F100_AllAcademies = 17181) Or (FundingBasis = 2 And F100_AllAcademies = 17182) Or (FundingBasis = 2 And F100_AllAcademies = 17183) then
            result = calc
        Else
            If(FundingBasis = 1 And F100_AllAcademies = 17182) Or (FundingBasis = 1 And F100_AllAcademies = 17183) then 'new opener
                If P170_EAL3SecRate > 0
                    Result = EAL_S
                Else
                    Result = 0
                End If
            End If
        End If

        Print(F100_AllAcademies, "F100_AllAcademies", rid)
        Print(FundingBasis, "FundingBasis", rid)
        Print(F200_SBS_Academies, "F200_SBS_Academies", rid)
        Print(P170_EAL3SecRate, "P170_EAL3SecRate", rid)
        Print(P168_EAL3SecFactor, "P168_EAL3SecFactor", rid)
        Print(P25_Total_NOR_SEC_SBS, "P25_Total_NOR_SEC_SBS", rid)
        Print(EAL_S, "EAL_S", rid)
        Print(calc, "calc", rid)
        Return result
    End Function

    <Calculation(Id:="d9bd1daab6724e148b5986df9c626d43")>
    <CalculationSpecification(Id:="P172_NSENSecEAL", Name:="P172_NSENSecEAL")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="dfd825e4a5784fa188379b080fa0c4ae", Name:="EAL")>
    Public Function P172_NSENSecEAL As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim F100_AllAcademies As Decimal = F100_AllAcademies
        Dim FundingBasis As Decimal = F900_FundingBasis
        Dim F200_SBS_Academies As Decimal = F200_SBS_Academies
        Dim P161_EAL1SecSubtotal As Decimal = P161_EAL1SecSubtotal
        Dim P166_EAL2SecSubtotal As Decimal = P166_EAL2SecSubtotal
        Dim P171_EAL3SecSubtotal As Decimal = P171_EAL3SecSubtotal
        Dim P172a_NSENSecEAL_Percent As Decimal = P172a_NSENSecEAL_Percent / 100
        If F200_SBS_Academies = 1 then
            Result =(P161_EAL1SecSubtotal + P166_EAL2SecSubtotal + P171_EAL3SecSubtotal) * P172a_NSENSecEAL_Percent
        Else
            exclude(rid)
        End If

        Print(F100_AllAcademies, "F100_AllAcademies", rid)
        Print(FundingBasis, "FundingBasis", rid)
        Print(F200_SBS_Academies, "F200_SBS_Academies", rid)
        Print(P161_EAL1SecSubtotal, "P161_EAL1SecSubtotal", rid)
        Print(P166_EAL2SecSubtotal, "P166_EAL2SecSubtotal", rid)
        Print(P171_EAL3SecSubtotal, "P171_EAL3SecSubtotal", rid)
        Print(P172a_NSENSecEAL_Percent, "P172a_NSENSecEAL_Percent", rid)
        Return result
    End Function

    <Calculation(Id:="e7e6599b52504ce3854778dde243f8c5")>
    <CalculationSpecification(Id:="P172a_NSENSecEAL_Percent", Name:="P172a_NSENSecEAL_Percent")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="dfd825e4a5784fa188379b080fa0c4ae", Name:="EAL")>
    Public Function P172a_NSENSecEAL_Percent As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim F100_AllAcademies As Decimal = F100_AllAcademies
        Dim FundingBasis As String = Datasets.AcademyInformation.FundingBasis
        Dim F200_SBS_Academies As Decimal = F200_SBS_Academies
        Dim EAL_Secondary_Notional_SEN As Decimal = LaToProv(Datasets.APTProformadataset.EALSecondaryNotionalSEN)
        If F200_SBS_Academies = 1 then
            Result = EAL_Secondary_Notional_SEN * 100
        Else
            exclude(rid)
        End If

        Print(F100_AllAcademies, "F100_AllAcademies", rid)
        Print(FundingBasis, "FundingBasis", rid)
        Print(F200_SBS_Academies, "F200_SBS_Academies", rid)
        Print(EAL_Secondary_Notional_SEN, "EAL_Secondary_Notional_SEN", rid)
        Return result
    End Function

    <Calculation(Id:="6af25ad8ed4b4c06b9f21ddac9d3d41a")>
    <CalculationSpecification(Id:="P173_InYearEAL3SecSubtotal", Name:="P173_InYearEAL3SecSubtotal")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="dfd825e4a5784fa188379b080fa0c4ae", Name:="EAL")>
    Public Function P173_InYearEAL3SecSubtotal As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim F100_AllAcademies As Decimal = F100_AllAcademies
        Dim FundingBasis As Decimal = F900_FundingBasis
        Dim F200_SBS_Academies As Decimal = F200_SBS_Academies
        Dim P171_EAL3SecSubtotal As Decimal = P171_EAL3SecSubtotal
        Dim P001_1718DaysOpen As Decimal = P001_1718DaysOpen
        Dim Year_Days As Decimal = 365
        If F200_SBS_Academies = 1 then
            Result = P171_EAL3SecSubtotal * P001_1718DaysOpen / Year_Days
        ELSE
            Exclude(rid)
        End IF

        Print(F100_AllAcademies, "F100_AllAcademies", rid)
        Print(FundingBasis, "FundingBasis", rid)
        Print(F200_SBS_Academies, "F200_SBS_Academies", rid)
        Print(P171_EAL3SecSubtotal, "P171_EAL3SecSubtotal", rid)
        Print(P001_1718DaysOpen, "P001_1718DaysOpen", rid)
        Print(Year_Days, "Year_Days", rid)
        Return result
    End Function

    <Calculation(Id:="0d947e712b264e288fb02d5ab1faa7f1")>
    <CalculationSpecification(Id:="P019_PriFSMFactor", Name:="P019_PriFSMFactor")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="524c10bb461b46f3b351b86403168379", Name:="FSM")>
    Public Function P019_PriFSMFactor As Decimal
        Dim result = Decimal.Zero
        Dim F200_SBS_Academies As Decimal = F200_SBS_Academies
        Dim FSMCensus As Decimal = Datasets.CensusPupilCharacteristics.PrimaryFSMProportion
        Dim FSMAdj As Decimal = Datasets.APTInputsandAdjustments.PrimaryFSMProportion
        Dim FSMAdjString as string = Datasets.APTInputsandAdjustments.PrimaryFSMProportion
        Dim FSMPriCensusString as string = Datasets.CensusPupilCharacteristics.PrimaryFSMProportion
        Dim LA_AV As Decimal = latoprov(Datasets.LocalAuthorityAverages.PrimaryFSMProportion)
        If(F200_SBS_Academies = 1) Then
            Print(LA_AV, "LA average", rid)
            Print(FSMCensus, "FSMPri Census", rid)
            Print(FSMAdj, "FSM Pri Adjusted", rid)
            If string.IsNullOrEmpty(FSMPriCensusString) And string.IsNullOrEmpty(FSMAdjString) THEN
                Result = LA_AV
            Elseif string.IsNullOrEmpty(FSMAdjString) then
                Result = FSMCensus
            Else
                Result = FSMAdj
            End if
        else
            exclude(rid)
        End If

        Return result
    End Function

    <Calculation(Id:="35cbaa6b1d84461bba50e385715b3a46")>
    <CalculationSpecification(Id:="P021_PriFSMRate", Name:="P021_PriFSMRate")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="524c10bb461b46f3b351b86403168379", Name:="FSM")>
    Public Function P021_PriFSMRate As Decimal
        Dim result = Decimal.Zero
        Dim F200_SBS_Academies As Decimal = F200_SBS_Academies
        If(F200_SBS_Academies = 1) Then
            Dim P021_PriFSMRate As Decimal = LAtoProv(i.)
            Dim LAMethod as string = LAtoProv(datasets.Academy_Allocations.AY_2017 / 18.APT.APT_Proforma_dataset.APT_Proforma_dataset.FSM_Primary_FSM / FSM6)
            Print(P021_PriFSMRate, "Pri FSM Per Pupil", rid)
            Print(LAMethod, "FSM/FSM6?", rid)
            If LAMethod = "FSM % Primary" then
                Result = P021_PriFSMRate
            Else
                Result = 0
            end if
        Else
            exclude(rid)
        End if

        Return result
    End Function

    <Calculation(Id:="fd1dbad17c2945c087a4641e6d58d9dc")>
    <CalculationSpecification(Id:="P022_PriFSMSubtotal", Name:="P022_PriFSMSubtotal")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="524c10bb461b46f3b351b86403168379", Name:="FSM")>
    Public Function P022_PriFSMSubtotal As Decimal
        Dim result = Decimal.Zero
        Dim F100_AllAcademies As Decimal = F100_AllAcademies
        Dim F900_FundingBasis As String = F900_FundingBasis
        Dim P22_Total_NOR_Pri_SBS As Decimal = NOR_P22_Total_NOR_PRI_SBS
        Dim P021_PriFSMRate As Decimal = P021_PriFSMRate
        Dim P019_PriFSMFactor As Decimal = P019_PriFSMFactor
        Dim P022_PriFSMSubtotal As Decimal = P22_Total_NOR_Pri_SBS * P021_PriFSMRate * P019_PriFSMFactor
        Dim FSMSelectedbyLA As String = LaToProv(Datasets.APTProformadataset.FSMPrimaryFSMFSM6)
        Dim P022_PriFSMSubtotalAPT As Decimal = Datasets.APTNewISBdataset.FreeSchoolMealsPrimary
        Print(F100_AllAcademies, "F100_AllAcademies", rid)
        Print(F900_FundingBasis, "F900_FundingBasis", rid)
        Print(P22_Total_NOR_Pri_SBS, "P22_Total_NOR_Pri_SBS", rid)
        Print(P021_PriFSMRate, "P021_PriFSMRate", rid)
        Print(P019_PriFSMFactor, "P019_PriFSMFactor", rid)
        If F900_FundingBasis = 3 Then
            exclude(rid)
        Else
            If(F100_AllAcademies = 17181) Or (F100_AllAcademies = 17182 And F900_FundingBasis = 2) Or (F100_AllAcademies = 17183 And F900_FundingBasis = 2) Then
                Result = P022_PriFSMSubtotal
            Else
                If(F100_AllAcademies = 17182 And F900_FundingBasis = 1) Or (F100_AllAcademies = 17183 And F900_FundingBasis = 1) Then
                    If FSMSelectedbyLA = "FSM % Primary" Then
                        Result = P022_PriFSMSubtotalAPT
                    End If
                Else
                End If

                Exclude(rid)
            End If
        End If

        Return result
    End Function

    <Calculation(Id:="910ec2516e05414f81e527f7f900ff31")>
    <CalculationSpecification(Id:="P023_InYearPriFSMSubtotal", Name:="P023_InYearPriFSMSubtotal")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="524c10bb461b46f3b351b86403168379", Name:="FSM")>
    Public Function P023_InYearPriFSMSubtotal As Decimal
        Dim result = Decimal.Zero
        Dim F200_SBS_Academies As Decimal = F200_SBS_Academies
        Dim F900_FundingBasis As String = F900_FundingBasis
        Dim P022_PriFSMSubtotal As Decimal = P022_PriFSMSubtotal
        Dim P001_1718DaysOpen As Decimal = P001_1718DaysOpen
        Dim Year_Days As Decimal = 365
        Print(F200_SBS_Academies, "F200_SBS_Academies", rid)
        Print(F900_FundingBasis, "F900_FundingBasis", rid)
        Print(P001_1718DaysOpen, "P001_1718DaysOpen", rid)
        Print(Year_Days, "Year_Days", rid)
        If F200_SBS_Academies = 1 Then
            Result =(P022_PriFSMSubtotal) * P001_1718DaysOpen / Year_Days
        Else
            Exclude(rid)
        End If

        Return result
    End Function

    <Calculation(Id:="efda4d93d365441985d46f93db684f33")>
    <CalculationSpecification(Id:="P024_PriFSM6Factor", Name:="P024_PriFSM6Factor")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="524c10bb461b46f3b351b86403168379", Name:="FSM")>
    Public Function P024_PriFSM6Factor As Decimal
        Dim result = Decimal.Zero
        Dim F200_SBS_Academies As Decimal = F200_SBS_Academies
        Dim F900_FundingBasis As String = F900_FundingBasis
        Dim FSM6Census As Decimal = Datasets.CensusPupilCharacteristics.PrimaryEver6Proportion
        Dim FSM6Adj As Decimal = Datasets.APTInputsandAdjustments.PrimaryEver6Proportion
        Dim FSM6AdjString as string = Datasets.APTInputsandAdjustments.PrimaryEver6Proportion
        Dim FSM6PriCensusString as string = Datasets.CensusPupilCharacteristics.PrimaryEver6Proportion
        Dim LA_AV As Decimal = latoprov(Datasets.LocalAuthorityAverages.PrimaryEver6Proportion)
        Print(F200_SBS_Academies, "F200_SBS_Academies", rid)
        Print(F900_FundingBasis, "F900_FundingBasis", rid)
        Print(LA_AV, "LA average", rid)
        Print(FSM6Census, "FSM6 Pri Census", rid)
        Print(FSM6Adj, "FSM6 Pri Adjusted", rid)
        If F900_FundingBasis = 3 Then
            exclude(rid)
        Else
            If F200_SBS_Academies = 1 Then
                If string.IsNullOrEmpty(FSM6PriCensusString) And string.IsNullOrEmpty(FSM6AdjString) Then
                    Result = LA_AV
                Else
                    If string.IsNullOrEmpty(FSM6AdjString) Then
                        Result = FSM6Census
                    Else
                        Result = FSM6Adj
                    End if
                End if
            Else
                Exclude(rid)
            End If
        End If

        Return result
    End Function

    <Calculation(Id:="218989d47c7d4fce97397b4b5d39db34")>
    <CalculationSpecification(Id:="P026_PriFSM6Rate", Name:="P026_PriFSM6Rate")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="524c10bb461b46f3b351b86403168379", Name:="FSM")>
    Public Function P026_PriFSM6Rate As Decimal
        Dim result = Decimal.Zero
        Dim F200_SBS_Academies As Decimal = F200_SBS_Academies
        Dim F900_FundingBasis As String = F900_FundingBasis
        Dim P026_PriFSM6Rate As Decimal = LAtoProv(Datasets.APTProformadataset.FSMPrimaryAmountPerPupil)
        Dim LAMethod as string = LAtoProv(Datasets.APTProformadataset.FSMPrimaryFSMFSM6)
        Print(F200_SBS_Academies, "F200_SBS_Academies", rid)
        Print(F900_FundingBasis, "F900_FundingBasis", rid)
        Print(P026_PriFSM6Rate, "Pri FSM6 Per Pupil", rid)
        Print(LAMethod, "FSM/FSM6?", rid)
        If F900_FundingBasis = 3 Then
            exclude(rid)
        Else
            If F200_SBS_Academies = 1 Then
                If LAMethod = "FSM6 % Primary" then
                    Result = P026_PriFSM6Rate
                Else
                    Result = 0
                End if
            Else
                exclude(rid)
            End if
        End If

        Return result
    End Function

    <Calculation(Id:="285ee6902a3a4b11bcaea27ceb2c420a")>
    <CalculationSpecification(Id:="P027_PriFSM6Subtotal", Name:="P027_PriFSM6Subtotal")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="524c10bb461b46f3b351b86403168379", Name:="FSM")>
    Public Function P027_PriFSM6Subtotal As Decimal
        Dim result = Decimal.Zero
        Dim F100_AllAcademies As Decimal = F100_AllAcademies
        Dim F900_FundingBasis As String = F900_FundingBasis
        Dim P22_Total_NOR_Pri_SBS As Decimal = NOR_P22_Total_NOR_PRI_SBS
        Dim P026_PriFSM6Rate As Decimal = P026_PriFSM6Rate
        Dim P024_PriFSM6Factor As Decimal = P024_PriFSM6Factor
        Dim P027_PriFSM6Subtotal As Decimal = P22_Total_NOR_Pri_SBS * P026_PriFSM6Rate * P024_PriFSM6Factor
        Dim FSM6SelectedbyLA As String = LaToProv(Datasets.APTProformadataset.FSMPrimaryFSMFSM6)
        Dim P027_PriFSM6SubtotalAPT As Decimal = Datasets.APTNewISBdataset.FreeSchoolMealsPrimary
        Print(F100_AllAcademies, "F100_AllAcademies", rid)
        Print(F900_FundingBasis, "F900_FundingBasis", rid)
        Print(P22_Total_NOR_Pri_SBS, "P22_Total_NOR_Pri_SBS", rid)
        Print(P026_PriFSM6Rate, "P026_PriFSM6Rate", rid)
        Print(P024_PriFSM6Factor, "P024_PriFSM6Factor", rid)
        If F900_FundingBasis = 3 Then
            exclude(rid)
        Else
            If(F100_AllAcademies = 17181) Or (F100_AllAcademies = 17182 And F900_FundingBasis = 2) Or (F100_AllAcademies = 17183 And F900_FundingBasis = 2) Then
                Result = P027_PriFSM6Subtotal
            Else
                If(F100_AllAcademies = 17182 And F900_FundingBasis = 1) Or (F100_AllAcademies = 17183 And F900_FundingBasis = 2) Then
                    If FSM6SelectedbyLA = "FSM6 % Primary" Then
                        Result = P027_PriFSM6SubtotalAPT
                    End If
                Else
                End If

                Exclude(rid)
            End If
        End If

        Return result
    End Function

    <Calculation(Id:="04abb412cefd45db8af379ad69055fa2")>
    <CalculationSpecification(Id:="P028_NSENFSMPri", Name:="P028_NSENFSMPri")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="524c10bb461b46f3b351b86403168379", Name:="FSM")>
    Public Function P028_NSENFSMPri As Decimal
        Dim result = Decimal.Zero
        Dim F200_SBS_Academies As Decimal = F200_SBS_Academies
        Dim F900_FundingBasis As String = F900_FundingBasis
        Dim PriFSMSubtotal As Decimal = P022_PriFSMSubtotal
        Dim PriFSM6Subtotal As Decimal = P027_PriFSM6Subtotal
        Dim FSM_Pri_SEN As Decimal = P028a_NSENFSMPri_Percent / 100
        Dim NSENFSMPri As Decimal =(PriFSMSubtotal + PriFSM6Subtotal) * FSM_Pri_SEN
        Print(F200_SBS_Academies, "F200_SBS_Academies", rid)
        Print(F900_FundingBasis, "FundingBasis", rid)
        Print(PriFSMSubtotal, "P022_PriFSMSubtotal", rid)
        Print(PriFSM6Subtotal, "P027_PriFSM6Subtotal", rid)
        Print(FSM_Pri_SEN, "FSM_Pri_SEN", rid)
        If F900_FundingBasis = 3 Then
            exclude(rid)
        Else
            If F200_SBS_Academies = 1 Then
                Result = NSENFSMPri
            else
                exclude(rid)
            End If
        End If

        Return result
    End Function

    <Calculation(Id:="ab7657ed01b34b8290f3c541fda73232")>
    <CalculationSpecification(Id:="P028a_NSENFSMPri_Percent", Name:="P028a_NSENFSMPri_Percent")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="524c10bb461b46f3b351b86403168379", Name:="FSM")>
    Public Function P028a_NSENFSMPri_Percent As Decimal
        Dim result = Decimal.Zero
        Dim F200_SBS_Academies As Decimal = F200_SBS_Academies
        Dim F900_FundingBasis As String = F900_FundingBasis
        Dim FSM_Pri_SEN As Decimal = LaToProv(Datasets.APTProformadataset.FSMPrimaryNotionalSEN)
        Dim NSENFSMPri_Percent As Decimal = FSM_Pri_SEN
        Print(F200_SBS_Academies, "F200_SBS_Academies", rid)
        Print(F900_FundingBasis, "F900_FundingBasis", rid)
        If F900_FundingBasis = 3 Then
            exclude(rid)
        Else
            If F200_SBS_Academies = 1 Then
                Result = NSENFSMPri_Percent * 100
            else
                exclude(rid)
            End If
        End If

        Return result
    End Function

    <Calculation(Id:="6afcc5151ddb4bd59fddb724b279268c")>
    <CalculationSpecification(Id:="P029_InYearPriFSM6Subtotal", Name:="P029_InYearPriFSM6Subtotal")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="524c10bb461b46f3b351b86403168379", Name:="FSM")>
    Public Function P029_InYearPriFSM6Subtotal As Decimal
        Dim result = Decimal.Zero
        Dim F200_SBS_Academies As Decimal = F200_SBS_Academies
        Dim F900_FundingBasis As String = F900_FundingBasis
        Dim P027_PriFSM6Subtotal As Decimal = P027_PriFSM6Subtotal
        Dim P001_1718DaysOpen As Decimal = P001_1718DaysOpen
        Dim Year_Days As Decimal = 365
        Print(F200_SBS_Academies, "F200_SBS_Academies", rid)
        Print(F900_FundingBasis, "F900_FundingBasis", rid)
        Print(P001_1718DaysOpen, "P001_1718DaysOpen", rid)
        Print(Year_Days, "Year_Days", rid)
        If F900_FundingBasis = 3 Then
            exclude(rid)
        Else
            If F200_SBS_Academies = 1 Then
                Result =(P027_PriFSM6Subtotal) * P001_1718DaysOpen / Year_Days
            Else
                Exclude(rid)
            End If
        End If

        Return result
    End Function

    <Calculation(Id:="e44d20ab226448b18e675c3678aa42e0")>
    <CalculationSpecification(Id:="P030_SecFSMFactor", Name:="P030_SecFSMFactor")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="524c10bb461b46f3b351b86403168379", Name:="FSM")>
    Public Function P030_SecFSMFactor As Decimal
        Dim result = Decimal.Zero
        Dim F200_SBS_Academies As Decimal = F200_SBS_Academies
        Dim F900_FundingBasis As String = F900_FundingBasis
        Dim FSMCensus As Decimal = Datasets.CensusPupilCharacteristics.SecondaryFSMProportion
        Dim FSMAdj As Decimal = Datasets.APTInputsandAdjustments.SecondaryFSMProportion
        Dim FSMAdjString as string = Datasets.APTInputsandAdjustments.SecondaryFSMProportion
        Dim FSMCensusCensusString as string = Datasets.CensusPupilCharacteristics.SecondaryFSMProportion
        Dim LA_AV As Decimal = latoprov(Datasets.LocalAuthorityAverages.SecondaryFSMProportion)
        Print(F200_SBS_Academies, "F200_SBS_Academies", rid)
        Print(F900_FundingBasis, "F900_FundingBasis", rid)
        Print(LA_AV, "LA average", rid)
        Print(FSMCensus, "FSM Sec Census", rid)
        Print(FSMAdj, "FSMAdj", rid)
        If F900_FundingBasis = 3 Then
            exclude(rid)
        Else
            If F200_SBS_Academies = 1 Then
                If string.IsNullOrEmpty(FSMCensusCensusString) And string.IsNullOrEmpty(FSMAdjString) Then
                    Result = LA_AV
                ElseIf string.IsNullOrEmpty(FSMAdjString) Then
                    result = FSMCensus
                else
                    result = FSMAdj
                End if
            else
                exclude(rid)
            End if
        End If

        Return result
    End Function

    <Calculation(Id:="7f49b25a2a3445b2864f23b4abf3f718")>
    <CalculationSpecification(Id:="P032_SecFSMRate", Name:="P032_SecFSMRate")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="524c10bb461b46f3b351b86403168379", Name:="FSM")>
    Public Function P032_SecFSMRate As Decimal
        Dim result = Decimal.Zero
        Dim F200_SBS_Academies As Decimal = F200_SBS_Academies
        Dim F900_FundingBasis As String = F900_FundingBasis
        Dim P032_SecFSMRate As Decimal = LAtoProv(Datasets.APTProformadataset.FSMSecondaryAmountPerPupil)
        Dim LAMethod as string = LAtoProv(Datasets.APTProformadataset.FSMSecondaryFSMFSM6)
        Print(F200_SBS_Academies, "F200_SBS_Academies", rid)
        Print(F900_FundingBasis, "F900_FundingBasis", rid)
        Print(P032_SecFSMRate, "Sec FSM Per Pupil", rid)
        Print(LAMethod, "FSM/FSM6?", rid)
        If F900_FundingBasis = 3 Then
            exclude(rid)
        Else
            If F200_SBS_Academies = 1 Then
                If LAMethod = "FSM % Secondary" then
                    Result = P032_SecFSMRate
                Else
                    Result = 0
                End if
            Else
                Exclude(rid)
            End if
        End If

        Return result
    End Function

    <Calculation(Id:="8ee714be039f47dcb1e73384f235de7b")>
    <CalculationSpecification(Id:="P033_SecFSMSubtotal", Name:="P033_SecFSMSubtotal")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="524c10bb461b46f3b351b86403168379", Name:="FSM")>
    Public Function P033_SecFSMSubtotal As Decimal
        Dim result = Decimal.Zero
        Dim F100_AllAcademies As Decimal = F100_AllAcademies
        Dim F900_FundingBasis As String = F900_FundingBasis
        Dim P25_TotalNORSecSBS As Decimal = NOR_P25_Total_NOR_SEC_SBS
        Dim P032_SecFSMRate As Decimal = P032_SecFSMRate
        Dim P030_SecFSMFactor As Decimal = P030_SecFSMFactor
        Dim P033_SecFSMSubtotal As Decimal = P25_TotalNORSecSBS * P032_SecFSMRate * P030_SecFSMFactor
        Dim P001_1718DaysOpen As Decimal = P001_1718DaysOpen
        Dim FSMSelectedbyLA As String = LaToProv(Datasets.APTProformadataset.FSMSecondaryFSMFSM6)
        Dim P033_SecFSMSubtotalAPT As Decimal = Datasets.APTNewISBdataset.FreeSchoolMealsSecondary
        Print(F100_AllAcademies, "F100_AllAcademies", rid)
        Print(F900_FundingBasis, "F900_FundingBasis", rid)
        Print(P25_TotalNORSecSBS, "P25_TotalNORSecSBS", rid)
        Print(P032_SecFSMRate, "P032_SecFSMRate", rid)
        Print(P030_SecFSMFactor, "P030_SecFSMFactor", rid)
        Print(FSMSelectedbyLA, "FSMSelectedbyLA", rid)
        Print(P033_SecFSMSubtotalAPT, "P033_SecFSMSubtotalAPT", rid)
        If F900_FundingBasis = 3 Then
            Exclude(rid)
        Else
        End if

        If F100_AllAcademies = 17181 Or (F100_AllAcademies = 17182 And F900_FundingBasis = 2) Or (F100_AllAcademies = 17183 And F900_FundingBasis = 2) Then
            Result = P033_SecFSMSubtotal
        Else
            If(F100_AllAcademies = 17182 And F900_FundingBasis = 1) Or (F100_AllAcademies = 17183 And F900_FundingBasis = 1) Then
                If FSMSelectedbyLA = "FSM % Secondary" Then
                    Result = P033_SecFSMSubtotalAPT
                Else
                    Result = 0
                End If
            Else
                Exclude(rid)
            End If
        End if

        Return result
    End Function

    <Calculation(Id:="e023312986c64b2e92d29ae770c002a4")>
    <CalculationSpecification(Id:="P034_InYearSecFSMSubtotal", Name:="P034_InYearSecFSMSubtotal")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="524c10bb461b46f3b351b86403168379", Name:="FSM")>
    Public Function P034_InYearSecFSMSubtotal As Decimal
        Dim result = Decimal.Zero
        Dim F200_SBS_Academies As Decimal = F200_SBS_Academies
        Dim F900_FundingBasis As String = F900_FundingBasis
        Dim P033_SecFSMSubtotal As Decimal = P033_SecFSMSubtotal
        Dim P001_1718DaysOpen As Decimal = P001_1718DaysOpen
        Dim Year_Days As Decimal = 365
        Print(F200_SBS_Academies, "F200_SBS_Academies", rid)
        Print(F900_FundingBasis, "F900_FundingBasis", rid)
        Print(P001_1718DaysOpen, "Days_Open", rid)
        Print(Year_Days, "Year_Days", rid)
        If F900_FundingBasis = 3 Then
            exclude(rid)
        Else
            If F200_SBS_Academies = 1 Then
                Result =(P033_SecFSMSubtotal) * P001_1718DaysOpen / Year_Days
            Else
                Exclude(rid)
            End If
        End If

        Return result
    End Function

    <Calculation(Id:="dd549875e9994f2da5d315350e6205af")>
    <CalculationSpecification(Id:="P035_SecFSM6Factor", Name:="P035_SecFSM6Factor")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="524c10bb461b46f3b351b86403168379", Name:="FSM")>
    Public Function P035_SecFSM6Factor As Decimal
        Dim result = Decimal.Zero
        Dim F200_SBS_Academies As Decimal = F200_SBS_Academies
        Dim F900_FundingBasis As String = F900_FundingBasis
        Dim FSM6Census As Decimal = Datasets.CensusPupilCharacteristics.SecondaryEver6Proportion
        Dim FSM6Adj As Decimal = Datasets.APTInputsandAdjustments.SecondaryEver6Proportion
        Dim FSM6AdjString as string = Datasets.APTInputsandAdjustments.SecondaryEver6Proportion
        Dim FSM6SecCensusString as string = Datasets.CensusPupilCharacteristics.SecondaryEver6Proportion
        Dim LA_AV As Decimal = latoprov(Datasets.LocalAuthorityAverages.SecondaryEver6Proportion)
        Print(LA_AV, "LA average", rid)
        Print(FSM6Census, "FSM6 Sec Census", rid)
        Print(FSM6Adj, "FSM6 Sec Adjusted", rid)
        Print(F200_SBS_Academies, "F200_SBS_Academies", rid)
        Print(F900_FundingBasis, "F900_FundingBasis", rid)
        If F900_FundingBasis = 3 Then
            Exclude(rid)
        Else
            If F200_SBS_Academies = 1 Then
                If string.IsNullOrEmpty(FSM6SecCensusString) And string.IsNullOrEmpty(FSM6AdjString) Then
                    Result = LA_AV
                Else
                    If string.IsNullOrEmpty(FSM6AdjString) Then
                        Result = FSM6Census
                    Else
                        Result = FSM6Adj
                    End if
                End if
            Else
                Exclude(rid)
            End If
        End If

        Return result
    End Function

    <Calculation(Id:="33bc705fb9dd4f06b9ceb56ee2c86a73")>
    <CalculationSpecification(Id:="P037_SecFSM6Rate", Name:="P037_SecFSM6Rate")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="524c10bb461b46f3b351b86403168379", Name:="FSM")>
    Public Function P037_SecFSM6Rate As Decimal
        Dim result = Decimal.Zero
        Dim F200_SBS_Academies As Decimal = F200_SBS_Academies
        Dim F900_FundingBasis As String = F900_FundingBasis
        Dim P037_SecFSM6Rate As Decimal = LAtoProv(Datasets.APTProformadataset.FSMSecondaryAmountPerPupil)
        Dim LAMethod as string = LAtoProv(Datasets.APTProformadataset.FSMSecondaryFSMFSM6)
        Print(F200_SBS_Academies, "F200_SBS_Academies", rid)
        Print(F900_FundingBasis, "F900_FundingBasis", rid)
        Print(P037_SecFSM6Rate, "Sec FSM6 Per Pupil", rid)
        Print(LAMethod, "FSM/FSM6?", rid)
        If F900_FundingBasis = 3 Then
            exclude(rid)
        Else
            If F200_SBS_Academies = 1 Then
                If LAMethod = "FSM6 % Secondary" Then
                    Result = P037_SecFSM6Rate
                Else
                    Result = 0
                End if
            Else
                Exclude(rid)
            End if
        End If

        Return result
    End Function

    <Calculation(Id:="b751295ade1945df8c7c32addede1d21")>
    <CalculationSpecification(Id:="P038_SecFSM6Subtotal", Name:="P038_SecFSM6Subtotal")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="524c10bb461b46f3b351b86403168379", Name:="FSM")>
    Public Function P038_SecFSM6Subtotal As Decimal
        Dim result = Decimal.Zero
        Dim F100_AllAcademies As Decimal = F100_AllAcademies
        Dim F900_FundingBasis As String = F900_FundingBasis
        Dim P25_Total_NOR_Sec_SBS As Decimal = NOR_P25_Total_NOR_SEC_SBS
        Dim P037_SecFSM6Rate As Decimal = P037_SecFSM6Rate
        Dim P035_SecFSM6Factor As Decimal = P035_SecFSM6Factor
        Dim P038_SecFSM6Subtotal As Decimal = P25_Total_NOR_Sec_SBS * P037_SecFSM6Rate * P035_SecFSM6Factor
        Dim FSMSelectedbyLA As String = LaToProv(Datasets.APTProformadataset.FSMSecondaryFSMFSM6)
        Dim P038_SecFSM6SubtotalAPT As Decimal = Datasets.APTNewISBdataset.FreeSchoolMealsSecondary
        Print(P038_SecFSM6SubtotalAPT, "P038_SecFSM6SubtotalAPT", rid)
        Print(FSMSelectedbyLA, "FSMSelectedbyLA", rid)
        Print(P25_Total_NOR_Sec_SBS, "P25_Total_NOR_Sec_SBS", rid)
        Print(P037_SecFSM6Rate, "P037_SecFSM6Rate", rid)
        Print(P035_SecFSM6Factor, "P035_SecFSM6Factor", rid)
        Print(F100_AllAcademies, "F100_AllAcademies", rid)
        Print(F900_FundingBasis, "F900_FundingBasis", rid)
        If F900_FundingBasis = 3 Then
            Exclude(rid)
        Else
        End if

        If F100_AllAcademies = 17181 Or (F100_AllAcademies = 17182 And F900_FundingBasis = 2) Or (F100_AllAcademies = 17183 And F900_FundingBasis = 2) Then
            Result = P038_SecFSM6Subtotal
        Else
            If(F100_AllAcademies = 17182 And F900_FundingBasis = 1) Or (F100_AllAcademies = 17183 And F900_FundingBasis = 1) Then
                If FSMSelectedbyLA = "FSM6 % Secondary" Then
                    Result = P038_SecFSM6SubtotalAPT
                Else
                    Result = 0
                End If
            Else
                exclude(rid)
            End If
        End if

        Return result
    End Function

    <Calculation(Id:="24fa088656804aceab4de2d8b3b46025")>
    <CalculationSpecification(Id:="P039_NSENFSMSec", Name:="P039_NSENFSMSec")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="524c10bb461b46f3b351b86403168379", Name:="FSM")>
    Public Function P039_NSENFSMSec As Decimal
        Dim result = Decimal.Zero
        Dim F200_SBS_Academies As Decimal = F200_SBS_Academies
        Dim F900_FundingBasis As String = F900_FundingBasis
        Dim SecFSMSubtotal As Decimal = P033_SecFSMSubtotal
        Dim SecFSM6Subtotal As Decimal = P038_SecFSM6Subtotal
        Dim FSM_Sec_SEN As Decimal = P039a_NSENFSMSec_Percent / 100
        Dim NSENFSMSec As Decimal =(SecFSMSubtotal + SecFSM6Subtotal) * FSM_Sec_SEN
        Print(F200_SBS_Academies, "F200_SBS_Academies", rid)
        Print(F900_FundingBasis, "FundingBasis", rid)
        Print(SecFSMSubtotal, "P033_SecFSMSubtotal", rid)
        Print(SecFSM6Subtotal, "P038_SecFSM6Subtotal", rid)
        Print(FSM_Sec_SEN, "FSM_Sec_SEN", rid)
        If F900_FundingBasis = 3 Then
            exclude(rid)
        Else
            If F200_SBS_Academies = 1 Then
                Result = NSENFSMSec
            else
                exclude(rid)
            End If
        End If

        Return result
    End Function

    <Calculation(Id:="6e67872d8c574959881603e8c152f989")>
    <CalculationSpecification(Id:="P039a_NSENFSMSec_Percent", Name:="P039a_NSENFSMSec_Percent")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="524c10bb461b46f3b351b86403168379", Name:="FSM")>
    Public Function P039a_NSENFSMSec_Percent As Decimal
        Dim result = Decimal.Zero
        Dim F200_SBS_Academies As Decimal = F200_SBS_Academies
        Dim F900_FundingBasis As String = F900_FundingBasis
        Dim FSM_Sec_SEN As Decimal = LaToProv(Datasets.APTProformadataset.FSMSecondaryNotionalSEN)
        Dim NSENFSMSec_Percent As Decimal = FSM_Sec_SEN * 100
        Print(F200_SBS_Academies, "F200_SBS_Academies", rid)
        Print(F900_FundingBasis, "F900_FundingBasis", rid)
        If F900_FundingBasis = 3 Then
            Exclude(rid)
        Else
            If F200_SBS_Academies = 1 Then
                Result = NSENFSMSec_Percent
            else
                exclude(rid)
            End If
        End If

        Return result
    End Function

    <Calculation(Id:="8c27c257acf04b2cbcb07db8832b7f5b")>
    <CalculationSpecification(Id:="P040_InYearSecFSM6Subtotal", Name:="P040_InYearSecFSM6Subtotal")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="524c10bb461b46f3b351b86403168379", Name:="FSM")>
    Public Function P040_InYearSecFSM6Subtotal As Decimal
        Dim result = Decimal.Zero
        Dim F200_SBS_Academies As Decimal = F200_SBS_Academies
        Dim F900_FundingBasis As String = F900_FundingBasis
        Dim P038_SecFSM6Subtotal As Decimal = P038_SecFSM6Subtotal
        Dim P001_1718DaysOpen As Decimal = P001_1718DaysOpen
        Dim Year_Days As Decimal = 365
        Print(P001_1718DaysOpen, "P001_1718DaysOpen", rid)
        Print(Year_Days, "Year_Days", rid)
        Print(F200_SBS_Academies, "F200_SBS_Academies", rid)
        Print(F900_FundingBasis, "F900_FundingBasis", rid)
        If F900_FundingBasis = 3 Then
            Exclude(rid)
        Else
            If F200_SBS_Academies = 1 Then
                Result =(P038_SecFSM6Subtotal) * P001_1718DaysOpen / Year_Days
            Else
                Exclude(rid)
            End If
        End If

        Return result
    End Function

    <Calculation(Id:="5cd6440d4cc746cfaff39b433b66ded2")>
    <CalculationSpecification(Id:="P041_IDACIFPriFactor", Name:="P041_IDACIFPriFactor")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="86e2181721da4a669a09cccbafc502f1", Name:="IDACI")>
    Public Function P041_IDACIFPriFactor As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim [Census_Pupil_Characteristics_IDACI_Primary_Proportion_Band_F] As Decimal = Datasets.CensusPupilCharacteristics.IDACIPrimaryProportionBandF
        Dim [APT_Inputs_and_Adjustments_IDACI_Primary_Proportion_Band_F] As Decimal = Datasets.APTInputsandAdjustments.IDACIPrimaryProportionBandF
        Dim [Local_Authority_Averages_IDACI_Primary_Proportion_Band_F] As Decimal = LAtoProv(Datasets.LocalAuthorityAverages.IDACIPrimaryProportionBandF)
        Dim AcadFilter As Decimal = F200_SBS_Academies
        Dim IDACIFPriAdjString as string = Datasets.APTInputsandAdjustments.IDACIPrimaryProportionBandF
        Dim IDACIFPriCensusString as string = Datasets.CensusPupilCharacteristics.IDACIPrimaryProportionBandF
        If AcadFilter = 1 Then
            If string.IsNullOrEmpty(IDACIFPriAdjString) And string.IsNullOrEmpty(IDACIFPriCensusString) Then
                result = [Local_Authority_Averages_IDACI_Primary_Proportion_Band_F]
            Else
                If string.IsNullOrEmpty(IDACIFPriAdjString) Then
                    result = [Census_Pupil_Characteristics_IDACI_Primary_Proportion_Band_F]
                Else
                    result = [APT_Inputs_and_Adjustments_IDACI_Primary_Proportion_Band_F]
                End if
            End if
        Else
            exclude(rid)
        End if

        Print(Census_Pupil_Characteristics_IDACI_Primary_Proportion_Band_F, "Census_Pupil_Characteristics_IDACI_Primary_Proportion_Band_F", rid)
        Print(APT_Inputs_and_Adjustments_IDACI_Primary_Proportion_Band_F, "APT_Inputs_and_Adjustments_IDACI_Primary_Proportion_Band_F", rid)
        Print(Local_Authority_Averages_IDACI_Primary_Proportion_Band_F, "Local_Authority_Averages_IDACI_Primary_Proportion_Band_F", rid)
        Return result
    End Function

    <Calculation(Id:="092611e1ca334fe881c6a0a19274b3fa")>
    <CalculationSpecification(Id:="P043_IDACIFPriRate", Name:="P043_IDACIFPriRate")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="86e2181721da4a669a09cccbafc502f1", Name:="IDACI")>
    Public Function P043_IDACIFPriRate As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim APT_PP As Decimal = LAtoProv(Datasets.APTProformadataset.IDACIPrimaryBFAmountPerPupil)
        Dim AcadFilter As Decimal = F200_SBS_Academies
        If AcadFilter = 1 Then
            result = APT_PP
        Else
            exclude(rid)
        End if

        Print(APT_PP, "APT_PP", rid)
        Return result
    End Function

    <Calculation(Id:="1aae30a7354b45edb44a2389094c3493")>
    <CalculationSpecification(Id:="P044_IDACIFPriSubtotal", Name:="P044_IDACIFPriSubtotal")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="86e2181721da4a669a09cccbafc502f1", Name:="IDACI")>
    Public Function P044_IDACIFPriSubtotal As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim SBS_P22 As Decimal = NOR_P22_Total_NOR_PRI_SBS
        Dim P043_IDACIFPriRate As Decimal = P043_IDACIFPriRate
        Dim P041_IDACIFPriFactor As Decimal = P041_IDACIFPriFactor
        Dim APT_ISB_IDACIF_Primary As Decimal = Datasets.APTNewISBdataset.IDACI(PF)
        Dim F100_AllAcademies As Decimal = F100_AllAcademies
        Dim AcadFilter As Decimal = F200_SBS_Academies
        Dim FundingBasis As decimal = F900_FundingBasis
        If AcadFilter = 1 then
            If F100_AllAcademies = 17181 Or (F100_AllAcademies = 17182 And FundingBasis = 2) Or (F100_AllAcademies = 17183 And FundingBasis = 2) Then
                Result = SBS_P22 * P043_IDACIFPriRate * P041_IDACIFPriFactor
            ElseIf F100_AllAcademies <> 17181 And FundingBasis = 1 Then
                result = APT_ISB_IDACIF_Primary
            Else
                Exclude(rid)
            End If
        else
            exclude(rid)
        End if

        Print(SBS_P22, "SBS_P22", rid)
        Print(P043_IDACIFPriRate, "P043_IDACIFPriRate", rid)
        Print(P041_IDACIFPriFactor, "P041_IDACIFPriFactor", rid)
        Print(APT_ISB_IDACIF_Primary, "APT_ISB_IDACIF_Primary", rid)
        Print(F100_AllAcademies, "F100_AllAcademies", rid)
        Print(FundingBasis, "Funding_Basis", rid)
        Return result
    End Function

    <Calculation(Id:="51f110d2b05a40fc883f4b26e5e8604f")>
    <CalculationSpecification(Id:="P045_NSENIDACIFPri", Name:="P045_NSENIDACIFPri")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="86e2181721da4a669a09cccbafc502f1", Name:="IDACI")>
    Public Function P045_NSENIDACIFPri As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim P044_IDACIFPriSubtotal As Decimal = P044_IDACIFPriSubtotal
        Dim P045a_NSENIDACIFPri_Percent As Decimal = P045a_NSENIDACIFPri_Percent / 100
        Dim AcadFilter As Decimal = F200_SBS_Academies
        If AcadFilter = 1 Then
            result = P044_IDACIFPriSubtotal * P045a_NSENIDACIFPri_Percent
        Else
            exclude(rid)
        End if

        Print(P044_IDACIFPriSubtotal, "P044_IDACIFPriSubtotal", rid)
        Print(P045a_NSENIDACIFPri_Percent, "P045a_NSENIDACIFPri_Percent", rid)
        Return result
    End Function

    <Calculation(Id:="132e0a1c0d1f409d902d5d27a7547eae")>
    <CalculationSpecification(Id:="P045a_NSENIDACIFPri_Percent", Name:="P045a_NSENIDACIFPri_Percent")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="86e2181721da4a669a09cccbafc502f1", Name:="IDACI")>
    Public Function P045a_NSENIDACIFPri_Percent As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim Notional_SEN As Decimal = LAtoProv(Datasets.APTProformadataset.IDACIBFPrimaryNotionalSEN)
        Dim AcadFilter As Decimal = F200_SBS_Academies
        If AcadFilter = 1 then
            result = Notional_SEN * 100
        Else
            exclude(rid)
        End if

        Print(Notional_SEN, "Notional_SEN", rid)
        Return result
    End Function

    <Calculation(Id:="6503b906122d4c70b5f4af20bf19785e")>
    <CalculationSpecification(Id:="P046_InYearIDACIFPriSubtotal", Name:="P046_InYearIDACIFPriSubtotal")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="86e2181721da4a669a09cccbafc502f1", Name:="IDACI")>
    Public Function P046_InYearIDACIFPriSubtotal As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim P044_IDACIFPriSubtotal As Decimal = P044_IDACIFPriSubtotal
        Dim P001_1718DaysOpen As Decimal = P001_1718DaysOpen
        Dim Year_Days1718 As Decimal = P025_YearDays_1718
        Dim AcadFilter As Decimal = F200_SBS_Academies
        If AcadFilter = 1 Then
            result = P044_IDACIFPriSubtotal * (P001_1718DaysOpen / Year_Days1718)
        Else
            exclude(rid)
        End if

        Print(P044_IDACIFPriSubtotal, "P044_IDACIFPriSubtotal", rid)
        Print(P001_1718DaysOpen, "P001_1718DaysOpen", rid)
        Print(Year_Days1718, "Year_Days1718", rid)
        Return result
    End Function

    <Calculation(Id:="2d8691c700b9432d9a6661bc41ce8aae")>
    <CalculationSpecification(Id:="P047_IDACIEPriFactor", Name:="P047_IDACIEPriFactor")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="86e2181721da4a669a09cccbafc502f1", Name:="IDACI")>
    Public Function P047_IDACIEPriFactor As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim [Census_Pupil_Characteristics_IDACI_Primary_Proportion_Band_E] As Decimal = Datasets.CensusPupilCharacteristics.IDACIPrimaryProportionBandE
        Dim [APT_Inputs_and_Adjustments_IDACI_Primary_Proportion_Band_E] As Decimal = Datasets.APTInputsandAdjustments.IDACIPrimaryProportionBandE
        Dim [Local_Authority_Averages_IDACI_Primary_Proportion_Band_E] As Decimal = LAtoProv(Datasets.LocalAuthorityAverages.IDACIPrimaryProportionBandE)
        Dim AcadFilter As Decimal = F200_SBS_Academies
        Dim IDACIEPriAdjString as string = Datasets.APTInputsandAdjustments.IDACIPrimaryProportionBandE
        Dim IDACIEPriCensusString as string = Datasets.CensusPupilCharacteristics.IDACIPrimaryProportionBandE
        If AcadFilter = 1 Then
            If string.IsNullOrEmpty(IDACIEPriAdjString) And string.IsNullOrEmpty(IDACIEPriCensusString) Then
                result = [Local_Authority_Averages_IDACI_Primary_Proportion_Band_E]
            Else
                If string.IsNullOrEmpty(IDACIEPriAdjString) Then
                    result = [Census_Pupil_Characteristics_IDACI_Primary_Proportion_Band_E]
                Else
                    result = [APT_Inputs_and_Adjustments_IDACI_Primary_Proportion_Band_E]
                End if
            End if
        Else
            exclude(rid)
        End if

        Print(Census_Pupil_Characteristics_IDACI_Primary_Proportion_Band_E, "Census_Pupil_Characteristics_IDACI_Primary_Proportion_Band_E", rid)
        Print(APT_Inputs_and_Adjustments_IDACI_Primary_Proportion_Band_E, "APT_Inputs_and_Adjustments_IDACI_Primary_Proportion_Band_E", rid)
        Print(Local_Authority_Averages_IDACI_Primary_Proportion_Band_E, "Local_Authority_Averages_IDACI_Primary_Proportion_Band_E", rid)
        Return result
    End Function

    <Calculation(Id:="2d95663144af4ceb86c92442234210fa")>
    <CalculationSpecification(Id:="P049_IDACIEPriRate", Name:="P049_IDACIEPriRate")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="86e2181721da4a669a09cccbafc502f1", Name:="IDACI")>
    Public Function P049_IDACIEPriRate As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim APT_PP As Decimal = LAtoProv(Datasets.APTProformadataset.IDACIPrimaryBEAmountPerPupil)
        Dim AcadFilter As Decimal = F200_SBS_Academies
        If AcadFilter = 1 Then
            result = APT_PP
        Else
            exclude(rid)
        End if

        Print(APT_PP, "APT_PP", rid)
        Return result
    End Function

    <Calculation(Id:="332dc0621bda4a77bfd3f119bd2ad2c4")>
    <CalculationSpecification(Id:="P050_IDACIEPriSubtotal", Name:="P050_IDACIEPriSubtotal")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="86e2181721da4a669a09cccbafc502f1", Name:="IDACI")>
    Public Function P050_IDACIEPriSubtotal As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim SBS_P22 As Decimal = NOR_P22_Total_NOR_PRI_SBS
        Dim P049_IDACIEPriRate As Decimal = P049_IDACIEPriRate
        Dim P047_IDACIEPriFactor As Decimal = P047_IDACIEPriFactor
        Dim APT_ISB_IDACIE_Primary As Decimal = Datasets.APTNewISBdataset.IDACI(PE)
        Dim F100_AllAcademies As Decimal = F100_AllAcademies
        Dim AcadFilter As Decimal = F200_SBS_Academies
        Dim FundingBasis As decimal = F900_FundingBasis
        If AcadFilter = 1 then
            If F100_AllAcademies = 17181 Or (F100_AllAcademies = 17182 And FundingBasis = 2) Or (F100_AllAcademies = 17183 And FundingBasis = 2) Then
                Result = SBS_P22 * P049_IDACIEPriRate * P047_IDACIEPriFactor
            ElseIf F100_AllAcademies <> 17181 And FundingBasis = 1 Then
                result = APT_ISB_IDACIE_Primary
            Else
                Exclude(rid)
            End If
        else
            exclude(rid)
        End if

        Print(SBS_P22, "SBS_P22", rid)
        Print(P049_IDACIEPriRate, "P049_IDACIEPriRate", rid)
        Print(P047_IDACIEPriFactor, "P047_IDACIEPriFactor", rid)
        Print(APT_ISB_IDACIE_Primary, "APT_ISB_IDACIE_Primary", rid)
        Print(F100_AllAcademies, "F100_AllAcademies", rid)
        Print(FundingBasis, "Funding_Basis", rid)
        Return result
    End Function

    <Calculation(Id:="7d4a76bc3f994bc4b10d22e154366c85")>
    <CalculationSpecification(Id:="P051_NSENIDACIEPri", Name:="P051_NSENIDACIEPri")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="86e2181721da4a669a09cccbafc502f1", Name:="IDACI")>
    Public Function P051_NSENIDACIEPri As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim P050_IDACIEPriSubtotal As Decimal = P050_IDACIEPriSubtotal
        Dim P051a_NSENIDACIEPri_Percent As Decimal = P051a_NSENIDACIEPri_Percent / 100
        Dim AcadFilter As Decimal = F200_SBS_Academies
        If AcadFilter = 1 Then
            result = P050_IDACIEPriSubtotal * P051a_NSENIDACIEPri_Percent
        Else
            exclude(rid)
        End if

        Print(P050_IDACIEPriSubtotal, "P050_IDACIEPriSubtotal", rid)
        Print(P051a_NSENIDACIEPri_Percent, "P051a_NSENIDACIEPri_Percent", rid)
        Return result
    End Function

    <Calculation(Id:="c4ffe65186fb416cb5dcd19cf1a1d93e")>
    <CalculationSpecification(Id:="P051a_NSENIDACIEPri_Percent", Name:="P051a_NSENIDACIEPri_Percent")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="86e2181721da4a669a09cccbafc502f1", Name:="IDACI")>
    Public Function P051a_NSENIDACIEPri_Percent As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim Notional_SEN As Decimal = LAtoProv(Datasets.APTProformadataset.IDACIBEPrimaryNotionalSEN)
        Dim AcadFilter As Decimal = F200_SBS_Academies
        If AcadFilter = 1 then
            result = Notional_SEN * 100
        Else
            exclude(rid)
        End if

        Print(Notional_SEN, "Notional_SEN", rid)
        Return result
    End Function

    <Calculation(Id:="6bc6eb1a77fb49c0af9651d59a5b5065")>
    <CalculationSpecification(Id:="P052_InYearIDACIEPriSubtotal", Name:="P052_InYearIDACIEPriSubtotal")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="86e2181721da4a669a09cccbafc502f1", Name:="IDACI")>
    Public Function P052_InYearIDACIEPriSubtotal As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim P050_IDACIEPriSubtotal As Decimal = P050_IDACIEPriSubtotal
        Dim P001_1718DaysOpen As Decimal = P001_1718DaysOpen
        Dim Year_Days1718 As Decimal = P025_YearDays_1718
        Dim AcadFilter As Decimal = F200_SBS_Academies
        If AcadFilter = 1 Then
            result = P050_IDACIEPriSubtotal * (P001_1718DaysOpen / Year_Days1718)
        Else
            exclude(rid)
        End if

        Print(P050_IDACIEPriSubtotal, "P050_IDACIEPriSubtotal", rid)
        Print(P001_1718DaysOpen, "P001_1718DaysOpen", rid)
        Print(Year_Days1718, "Year_Days1718", rid)
        Return result
    End Function

    <Calculation(Id:="412091ed3d2b42ba8a6a5a48b32a77be")>
    <CalculationSpecification(Id:="P053_IDACIDPriFactor", Name:="P053_IDACIDPriFactor")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="86e2181721da4a669a09cccbafc502f1", Name:="IDACI")>
    Public Function P053_IDACIDPriFactor As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim [Census_Pupil_Characteristics_IDACI_Primary_Proportion_Band_D] As Decimal = Datasets.CensusPupilCharacteristics.IDACIPrimaryProportionBandD
        Dim [APT_Inputs_and_Adjustments_IDACI_Primary_Proportion_Band_D] As Decimal = Datasets.APTInputsandAdjustments.IDACIPrimaryProportionBandD
        Dim [Local_Authority_Averages_IDACI_Primary_Proportion_Band_D] As Decimal = LAtoProv(Datasets.LocalAuthorityAverages.IDACIPrimaryProportionBandD)
        Dim AcadFilter As Decimal = F200_SBS_Academies
        Dim IDACIDPriAdjString as string = Datasets.APTInputsandAdjustments.IDACIPrimaryProportionBandD
        Dim IDACIDPriCensusString as string = Datasets.CensusPupilCharacteristics.IDACIPrimaryProportionBandD
        If AcadFilter = 1 Then
            If string.IsNullOrEmpty(IDACIDPriAdjString) And string.IsNullOrEmpty(IDACIDPriCensusString) Then
                result = [Local_Authority_Averages_IDACI_Primary_Proportion_Band_D]
            Else
                If string.IsNullOrEmpty(IDACIDPriAdjString) Then
                    result = [Census_Pupil_Characteristics_IDACI_Primary_Proportion_Band_D]
                Else
                    result = [APT_Inputs_and_Adjustments_IDACI_Primary_Proportion_Band_D]
                End if
            End if
        Else
            exclude(rid)
        End if

        Print(Census_Pupil_Characteristics_IDACI_Primary_Proportion_Band_D, "Census_Pupil_Characteristics_IDACI_Primary_Proportion_Band_D", rid)
        Print(APT_Inputs_and_Adjustments_IDACI_Primary_Proportion_Band_D, "APT_Inputs_and_Adjustments_IDACI_Primary_Proportion_Band_D", rid)
        Print(Local_Authority_Averages_IDACI_Primary_Proportion_Band_D, "Local_Authority_Averages_IDACI_Primary_Proportion_Band_D", rid)
        Return result
    End Function

    <Calculation(Id:="355a1ddc7619482e8be1a6bf4267cde1")>
    <CalculationSpecification(Id:="P055_IDACIDPriRate", Name:="P055_IDACIDPriRate")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="86e2181721da4a669a09cccbafc502f1", Name:="IDACI")>
    Public Function P055_IDACIDPriRate As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim APT_PP As Decimal = LAtoProv(Datasets.APTProformadataset.IDACIPrimaryBDAmountPerPupil)
        Dim AcadFilter As Decimal = F200_SBS_Academies
        If AcadFilter = 1 Then
            result = APT_PP
        Else
            exclude(rid)
        End if

        Print(APT_PP, "APT_PP", rid)
        Return result
    End Function

    <Calculation(Id:="0d9e50e8310e4440bbc200b8e4021605")>
    <CalculationSpecification(Id:="P056_IDACIDPriSubtotal", Name:="P056_IDACIDPriSubtotal")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="86e2181721da4a669a09cccbafc502f1", Name:="IDACI")>
    Public Function P056_IDACIDPriSubtotal As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim SBS_P22 As Decimal = NOR_P22_Total_NOR_PRI_SBS
        Dim P055_IDACIDPriRate As Decimal = P055_IDACIDPriRate
        Dim P053_IDACIDPriFactor As Decimal = P053_IDACIDPriFactor
        Dim APT_ISB_IDACID_Primary As Decimal = Datasets.APTNewISBdataset.IDACI(PD)
        Dim F100_AllAcademies As Decimal = F100_AllAcademies
        Dim AcadFilter As Decimal = F200_SBS_Academies
        Dim FundingBasis As decimal = F900_FundingBasis
        If AcadFilter = 1 then
            If F100_AllAcademies = 17181 Or (F100_AllAcademies = 17182 And FundingBasis = 2) Or (F100_AllAcademies = 17183 And FundingBasis = 2) Then
                result = SBS_P22 * P055_IDACIDPriRate * P053_IDACIDPriFactor
            ElseIf F100_AllAcademies <> 17181 And FundingBasis = 1 Then
                If P055_IDACIDPriRate > 0 then
                    result = APT_ISB_IDACID_Primary
                else
                    result = 0
                End if
            Else
                Exclude(rid)
            End If
        else
            exclude(rid)
        End if

        Print(SBS_P22, "SBS_P22", rid)
        Print(P055_IDACIDPriRate, "P055_IDACIDPriRate", rid)
        Print(P053_IDACIDPriFactor, "P053_IDACIDPriFactor", rid)
        Print(APT_ISB_IDACID_Primary, "APT_ISB_IDACID_Primary", rid)
        Print(F100_AllAcademies, "F100_AllAcademies", rid)
        Print(FundingBasis, "Funding_Basis", rid)
        Return result
    End Function

    <Calculation(Id:="ab1b3f43646a4f6eb6e0593c911ee5cd")>
    <CalculationSpecification(Id:="P057_NSENIDACIDPri", Name:="P057_NSENIDACIDPri")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="86e2181721da4a669a09cccbafc502f1", Name:="IDACI")>
    Public Function P057_NSENIDACIDPri As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim P056_IDACIDPriSubtotal As Decimal = P056_IDACIDPriSubtotal
        Dim P057a_NSENIDACIDPri_Percent As Decimal = P057a_NSENIDACIDPri_Percent / 100
        Dim AcadFilter As Decimal = F200_SBS_Academies
        If AcadFilter = 1 Then
            result = P056_IDACIDPriSubtotal * P057a_NSENIDACIDPri_Percent
        Else
            exclude(rid)
        End if

        Print(P056_IDACIDPriSubtotal, "P056_IDACIDPriSubtotal", rid)
        Print(P057a_NSENIDACIDPri_Percent, "P057a_NSENIDACIDPri_Percent", rid)
        Return result
    End Function

    <Calculation(Id:="6b2eac66fbf24fc0b645502bbe2fdfd9")>
    <CalculationSpecification(Id:="P057a_NSENIDACIDPri_Percent", Name:="P057a_NSENIDACIDPri_Percent")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="86e2181721da4a669a09cccbafc502f1", Name:="IDACI")>
    Public Function P057a_NSENIDACIDPri_Percent As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim Notional_SEN As Decimal = LAtoProv(Datasets.APTProformadataset.IDACIBDPrimaryNotionalSEN)
        Dim AcadFilter As Decimal = F200_SBS_Academies
        If AcadFilter = 1 then
            result = Notional_SEN * 100
        Else
            exclude(rid)
        End if

        Print(Notional_SEN, "Notional_SEN", rid)
        Return result
    End Function

    <Calculation(Id:="5f91876552fe4b188c745658e3156ef5")>
    <CalculationSpecification(Id:="P058_InYearIDACIDPriSubtotal", Name:="P058_InYearIDACIDPriSubtotal")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="86e2181721da4a669a09cccbafc502f1", Name:="IDACI")>
    Public Function P058_InYearIDACIDPriSubtotal As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim P056_IDACIDPriSubtotal As Decimal = P056_IDACIDPriSubtotal
        Dim P001_1718DaysOpen As Decimal = P001_1718DaysOpen
        Dim Year_Days1718 As Decimal = P025_YearDays_1718
        Dim AcadFilter As Decimal = F200_SBS_Academies
        If AcadFilter = 1 Then
            result = P056_IDACIDPriSubtotal * (P001_1718DaysOpen / Year_Days1718)
        Else
            exclude(rid)
        End if

        Print(P056_IDACIDPriSubtotal, "P056_IDACIDPriSubtotal", rid)
        Print(P001_1718DaysOpen, "P001_1718DaysOpen", rid)
        Print(Year_Days1718, "Year_Days1718", rid)
        Return result
    End Function

    <Calculation(Id:="ece1f7c724f645be97161d1a69e0b146")>
    <CalculationSpecification(Id:="P059_IDACICPriFactor", Name:="P059_IDACICPriFactor")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="86e2181721da4a669a09cccbafc502f1", Name:="IDACI")>
    Public Function P059_IDACICPriFactor As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim [Census_Pupil_Characteristics_IDACI_Primary_Proportion_Band_C] As Decimal = Datasets.CensusPupilCharacteristics.IDACIPrimaryProportionBandC
        Dim [APT_Inputs_and_Adjustments_IDACI_Primary_Proportion_Band_C] As Decimal = Datasets.APTInputsandAdjustments.IDACIPrimaryProportionBandC
        Dim [Local_Authority_Averages_IDACI_Primary_Proportion_Band_C] As Decimal = LAtoProv(Datasets.LocalAuthorityAverages.IDACIPrimaryProportionBandC)
        Dim AcadFilter As Decimal = F200_SBS_Academies
        Dim IDACICPriAdjString as string = Datasets.APTInputsandAdjustments.IDACIPrimaryProportionBandC
        Dim IDACICPriCensusString as string = Datasets.CensusPupilCharacteristics.IDACIPrimaryProportionBandC
        If AcadFilter = 1 Then
            If string.IsNullOrEmpty(IDACICPriAdjString) And string.IsNullOrEmpty(IDACICPriCensusString) Then
                result = [Local_Authority_Averages_IDACI_Primary_Proportion_Band_C]
            Else
                If string.IsNullOrEmpty(IDACICPriAdjString) Then
                    result = [Census_Pupil_Characteristics_IDACI_Primary_Proportion_Band_C]
                Else
                    result = [APT_Inputs_and_Adjustments_IDACI_Primary_Proportion_Band_C]
                End if
            End if
        Else
            exclude(rid)
        End if

        Print(Census_Pupil_Characteristics_IDACI_Primary_Proportion_Band_C, "Census_Pupil_Characteristics_IDACI_Primary_Proportion_Band_C", rid)
        Print(APT_Inputs_and_Adjustments_IDACI_Primary_Proportion_Band_C, "APT_Inputs_and_Adjustments_IDACI_Primary_Proportion_Band_C", rid)
        Print(Local_Authority_Averages_IDACI_Primary_Proportion_Band_C, "Local_Authority_Averages_IDACI_Primary_Proportion_Band_C", rid)
        Return result
    End Function

    <Calculation(Id:="a4d8244f218f4e1fbb3fc26f7e6a3471")>
    <CalculationSpecification(Id:="P061_IDACICPriRate", Name:="P061_IDACICPriRate")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="86e2181721da4a669a09cccbafc502f1", Name:="IDACI")>
    Public Function P061_IDACICPriRate As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim APT_PP As Decimal = LAtoProv(Datasets.APTProformadataset.IDACIPrimaryBCAmountPerPupil)
        Dim AcadFilter As Decimal = F200_SBS_Academies
        If AcadFilter = 1 Then
            result = APT_PP
        Else
            exclude(rid)
        End if

        Print(APT_PP, "APT_PP", rid)
        Return result
    End Function

    <Calculation(Id:="91de4fd68fca4f05983f06901de5e44d")>
    <CalculationSpecification(Id:="P062_IDACICPriSubtotal", Name:="P062_IDACICPriSubtotal")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="86e2181721da4a669a09cccbafc502f1", Name:="IDACI")>
    Public Function P062_IDACICPriSubtotal As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim SBS_P22 As Decimal = NOR_P22_Total_NOR_PRI_SBS
        Dim P061_IDACICPriRate As Decimal = P061_IDACICPriRate
        Dim P059_IDACICPriFactor As Decimal = P059_IDACICPriFactor
        Dim APT_ISB_IDACIC_Primary As Decimal = Datasets.APTNewISBdataset.IDACI(PC)
        Dim F100_AllAcademies As Decimal = F100_AllAcademies
        Dim AcadFilter As Decimal = F200_SBS_Academies
        Dim FundingBasis As decimal = F900_FundingBasis
        If AcadFilter = 1 then
            If F100_AllAcademies = 17181 Or (F100_AllAcademies = 17182 And FundingBasis = 2) Or (F100_AllAcademies = 17183 And FundingBasis = 2) Then
                Result = SBS_P22 * P061_IDACICPriRate * P059_IDACICPriFactor
            ElseIf F100_AllAcademies <> 17181 And FundingBasis = 1 Then
                result = APT_ISB_IDACIC_Primary
            Else
                Exclude(rid)
            End If
        else
            exclude(rid)
        End if

        Print(SBS_P22, "SBS_P22", rid)
        Print(P061_IDACICPriRate, "P061_IDACICPriRate", rid)
        Print(P059_IDACICPriFactor, "P059_IDACICPriFactor", rid)
        Print(APT_ISB_IDACIC_Primary, "APT_ISB_IDACIC_Primary", rid)
        Print(F100_AllAcademies, "F100_AllAcademies", rid)
        Print(FundingBasis, "Funding_Basis", rid)
        Return result
    End Function

    <Calculation(Id:="ef5dabe1b8584307890c8533ea519752")>
    <CalculationSpecification(Id:="P063_NSENIDACICPri", Name:="P063_NSENIDACICPri")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="86e2181721da4a669a09cccbafc502f1", Name:="IDACI")>
    Public Function P063_NSENIDACICPri As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim P062_IDACICPriSubtotal As Decimal = P062_IDACICPriSubtotal
        Dim P063a_NSENIDACICPri_Percent As Decimal = P063a_NSENIDACICPri_Percent / 100
        Dim AcadFilter As Decimal = F200_SBS_Academies
        If AcadFilter = 1 Then
            result = P062_IDACICPriSubtotal * P063a_NSENIDACICPri_Percent
        Else
            exclude(rid)
        End if

        Print(P062_IDACICPriSubtotal, "P062_IDACICPriSubtotal", rid)
        Print(P063a_NSENIDACICPri_Percent, "P063a_NSENIDACICPri_Percent", rid)
        Return result
    End Function

    <Calculation(Id:="0767bd9f2dc945d19b79d050d45ecde6")>
    <CalculationSpecification(Id:="P063a_NSENIDACICPri_Percent", Name:="P063a_NSENIDACICPri_Percent")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="86e2181721da4a669a09cccbafc502f1", Name:="IDACI")>
    Public Function P063a_NSENIDACICPri_Percent As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim Notional_SEN As Decimal = LAtoProv(Datasets.APTProformadataset.IDACIBCPrimaryNotionalSEN)
        Dim AcadFilter As Decimal = F200_SBS_Academies
        If AcadFilter = 1 then
            result = Notional_SEN * 100
        Else
            exclude(rid)
        End if

        Print(Notional_SEN, "Notional_SEN", rid)
        Return result
    End Function

    <Calculation(Id:="07d675a781614b578f1212807d3df6f1")>
    <CalculationSpecification(Id:="P064_InYearIDACICPriSubtotal", Name:="P064_InYearIDACICPriSubtotal")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="86e2181721da4a669a09cccbafc502f1", Name:="IDACI")>
    Public Function P064_InYearIDACICPriSubtotal As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim P062_IDACICPriSubtotal As Decimal = P062_IDACICPriSubtotal
        Dim P001_1718DaysOpen As Decimal = P001_1718DaysOpen
        Dim Year_Days1718 As Decimal = P025_YearDays_1718
        Dim AcadFilter As Decimal = F200_SBS_Academies
        If AcadFilter = 1 Then
            result = P062_IDACICPriSubtotal * (P001_1718DaysOpen / Year_Days1718)
        Else
            exclude(rid)
        End if

        Print(P062_IDACICPriSubtotal, "P062_IDACICPriSubtotal", rid)
        Print(P001_1718DaysOpen, "P001_1718DaysOpen", rid)
        Print(Year_Days1718, "Year_Days1718", rid)
        Return result
    End Function

    <Calculation(Id:="aada298b96974270a650952181538d73")>
    <CalculationSpecification(Id:="P065_IDACIBPriFactor", Name:="P065_IDACIBPriFactor")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="86e2181721da4a669a09cccbafc502f1", Name:="IDACI")>
    Public Function P065_IDACIBPriFactor As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim [Census_Pupil_Characteristics_IDACI_Primary_Proportion_Band_B] As Decimal = Datasets.CensusPupilCharacteristics.IDACIPrimaryProportionBandB
        Dim [APT_Inputs_and_Adjustments_IDACI_Primary_Proportion_Band_B] As Decimal = Datasets.APTInputsandAdjustments.IDACIPrimaryProportionBandB
        Dim [Local_Authority_Averages_IDACI_Primary_Proportion_Band_B] As Decimal = LAtoProv(Datasets.LocalAuthorityAverages.IDACIPrimaryProportionBandB)
        Dim AcadFilter As Decimal = F200_SBS_Academies
        Dim IDACIBPriAdjString as string = Datasets.APTInputsandAdjustments.IDACIPrimaryProportionBandC
        Dim IDACIBPriCensusString as string = Datasets.CensusPupilCharacteristics.IDACIPrimaryProportionBandC
        If AcadFilter = 1 Then
            If string.IsNullOrEmpty(IDACIBPriAdjString) And string.IsNullOrEmpty(IDACIBPriCensusString) Then
                result = [Local_Authority_Averages_IDACI_Primary_Proportion_Band_B]
            Else
                If string.IsNullOrEmpty(IDACIBPriAdjString) Then
                    result = [Census_Pupil_Characteristics_IDACI_Primary_Proportion_Band_B]
                Else
                    result = [APT_Inputs_and_Adjustments_IDACI_Primary_Proportion_Band_B]
                End if
            End if
        Else
            exclude(rid)
        End if

        Print(Census_Pupil_Characteristics_IDACI_Primary_Proportion_Band_B, "Census_Pupil_Characteristics_IDACI_Primary_Proportion_Band_B", rid)
        Print(APT_Inputs_and_Adjustments_IDACI_Primary_Proportion_Band_B, "APT_Inputs_and_Adjustments_IDACI_Primary_Proportion_Band_B", rid)
        Print(Local_Authority_Averages_IDACI_Primary_Proportion_Band_B, "Local_Authority_Averages_IDACI_Primary_Proportion_Band_B", rid)
        Return result
    End Function

    <Calculation(Id:="539548a8021048f2bbc8a77ac1573394")>
    <CalculationSpecification(Id:="P067_IDACIBPriRate", Name:="P067_IDACIBPriRate")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="86e2181721da4a669a09cccbafc502f1", Name:="IDACI")>
    Public Function P067_IDACIBPriRate As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim APT_PP As Decimal = LAtoProv(Datasets.APTProformadataset.IDACIPrimaryBBAmountPerPupil)
        Dim AcadFilter As Decimal = F200_SBS_Academies
        If AcadFilter = 1 Then
            result = APT_PP
        Else
            exclude(rid)
        End if

        Print(APT_PP, "APT_PP", rid)
        Return result
    End Function

    <Calculation(Id:="b6c465ed28c5407796ca71837be2415e")>
    <CalculationSpecification(Id:="P068_IDACIBPriSubtotal", Name:="P068_IDACIBPriSubtotal")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="86e2181721da4a669a09cccbafc502f1", Name:="IDACI")>
    Public Function P068_IDACIBPriSubtotal As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim SBS_P22 As Decimal = NOR_P22_Total_NOR_PRI_SBS
        Dim P067_IDACIBPriRate As Decimal = P067_IDACIBPriRate
        Dim P065_IDACIBPriFactor As Decimal = P065_IDACIBPriFactor
        Dim APT_ISB_IDACIB_Primary As Decimal = Datasets.APTNewISBdataset.IDACI(PB)
        Dim F100_AllAcademies As Decimal = F100_AllAcademies
        Dim AcadFilter As Decimal = F200_SBS_Academies
        Dim FundingBasis As decimal = F900_FundingBasis
        If AcadFilter = 1 then
            If F100_AllAcademies = 17181 Or (F100_AllAcademies = 17182 And FundingBasis = 2) Or (F100_AllAcademies = 17183 And FundingBasis = 2) Then
                Result = SBS_P22 * P067_IDACIBPriRate * P065_IDACIBPriFactor
            ElseIf F100_AllAcademies <> 17181 And FundingBasis = 1 Then
                result = APT_ISB_IDACIB_Primary
            Else
                Exclude(rid)
            End If
        else
            exclude(rid)
        End if

        Print(SBS_P22, "SBS_P22", rid)
        Print(P067_IDACIBPriRate, "P067_IDACIBPriRate", rid)
        Print(P065_IDACIBPriFactor, "P065_IDACIBPriFactor", rid)
        Print(APT_ISB_IDACIB_Primary, "APT_ISB_IDACIB_Primary", rid)
        Print(F100_AllAcademies, "F100_AllAcademies", rid)
        Print(FundingBasis, "Funding_Basis", rid)
        Return result
    End Function

    <Calculation(Id:="3020a1411f894ab99827904a41514963")>
    <CalculationSpecification(Id:="P069_NSENIDACIBPri", Name:="P069_NSENIDACIBPri")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="86e2181721da4a669a09cccbafc502f1", Name:="IDACI")>
    Public Function P069_NSENIDACIBPri As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim P068_IDACIBPriSubtotal As Decimal = P068_IDACIBPriSubtotal
        Dim P069a_NSENIDACIBPri_Percent As Decimal = P069a_NSENIDACIBPri_Percent / 100
        Dim AcadFilter As Decimal = F200_SBS_Academies
        If AcadFilter = 1 Then
            result = P068_IDACIBPriSubtotal * P069a_NSENIDACIBPri_Percent
        Else
            exclude(rid)
        End if

        Print(P068_IDACIBPriSubtotal, "P068_IDACIBPriSubtotal", rid)
        Print(P069a_NSENIDACIBPri_Percent, "P069a_NSENIDACIBPri_Percent", rid)
        Return result
    End Function

    <Calculation(Id:="c055c049bdce442cb5cc4aaa56d0b031")>
    <CalculationSpecification(Id:="P069a_NSENIDACIBPri_Percent", Name:="P069a_NSENIDACIBPri_Percent")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="86e2181721da4a669a09cccbafc502f1", Name:="IDACI")>
    Public Function P069a_NSENIDACIBPri_Percent As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim Notional_SEN As Decimal = LAtoProv(Datasets.APTProformadataset.IDACIBBPrimaryNotionalSEN)
        Dim AcadFilter As Decimal = F200_SBS_Academies
        If AcadFilter = 1 then
            result = Notional_SEN * 100
        Else
            exclude(rid)
        End if

        Print(Notional_SEN, "Notional_SEN", rid)
        Return result
    End Function

    <Calculation(Id:="9a1ee4146382426f9ba251eb3ab4057d")>
    <CalculationSpecification(Id:="P070_InYearIDACIBPriSubtotal", Name:="P070_InYearIDACIBPriSubtotal")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="86e2181721da4a669a09cccbafc502f1", Name:="IDACI")>
    Public Function P070_InYearIDACIBPriSubtotal As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim P068_IDACIBPriSubtotal As Decimal = P068_IDACIBPriSubtotal
        Dim P001_1718DaysOpen As Decimal = P001_1718DaysOpen
        Dim Year_Days1718 As Decimal = P025_YearDays_1718
        Dim AcadFilter As Decimal = F200_SBS_Academies
        If AcadFilter = 1 Then
            result = P068_IDACIBPriSubtotal * (P001_1718DaysOpen / Year_Days1718)
        Else
            exclude(rid)
        End if

        Print(P068_IDACIBPriSubtotal, "P068_IDACIBPriSubtotal", rid)
        Print(P001_1718DaysOpen, "P001_1718DaysOpen", rid)
        Print(Year_Days1718, "Year_Days1718", rid)
        Return result
    End Function

    <Calculation(Id:="39b42b51af7f43b0bc038f36e00ee263")>
    <CalculationSpecification(Id:="P071_IDACIAPriFactor", Name:="P071_IDACIAPriFactor")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="86e2181721da4a669a09cccbafc502f1", Name:="IDACI")>
    Public Function P071_IDACIAPriFactor As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim [Census_Pupil_Characteristics_IDACI_Primary_Proportion_Band_A] As Decimal = Datasets.CensusPupilCharacteristics.IDACIPrimaryProportionBandA
        Dim [APT_Inputs_and_Adjustments_IDACI_Primary_Proportion_Band_A] As Decimal = Datasets.APTInputsandAdjustments.IDACIPrimaryProportionBandA
        Dim [Local_Authority_Averages_IDACI_Primary_Proportion_Band_A] As Decimal = LAtoProv(Datasets.LocalAuthorityAverages.IDACIPrimaryProportionBandA)
        Dim AcadFilter As Decimal = F200_SBS_Academies
        Dim IDACIAPriAdjString as string = Datasets.APTInputsandAdjustments.IDACIPrimaryProportionBandA
        Dim IDACIAPriCensusString as string = Datasets.CensusPupilCharacteristics.IDACIPrimaryProportionBandA
        If AcadFilter = 1 Then
            If string.IsNullOrEmpty(IDACIAPriAdjString) And string.IsNullOrEmpty(IDACIAPriCensusString) Then
                result = [Local_Authority_Averages_IDACI_Primary_Proportion_Band_A]
            Else
                If string.IsNullOrEmpty(IDACIAPriAdjString) Then
                    result = [Census_Pupil_Characteristics_IDACI_Primary_Proportion_Band_A]
                Else
                    result = [APT_Inputs_and_Adjustments_IDACI_Primary_Proportion_Band_A]
                End if
            End if
        Else
            exclude(rid)
        End if

        Print(Census_Pupil_Characteristics_IDACI_Primary_Proportion_Band_A, "Census_Pupil_Characteristics_IDACI_Primary_Proportion_Band_A", rid)
        Print(APT_Inputs_and_Adjustments_IDACI_Primary_Proportion_Band_A, "APT_Inputs_and_Adjustments_IDACI_Primary_Proportion_Band_A", rid)
        Print(Local_Authority_Averages_IDACI_Primary_Proportion_Band_A, "Local_Authority_Averages_IDACI_Primary_Proportion_Band_A", rid)
        Return result
    End Function

    <Calculation(Id:="d37ebf5271f14b0f8ae369f29222446b")>
    <CalculationSpecification(Id:="P073_IDACIAPriRate", Name:="P073_IDACIAPriRate")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="86e2181721da4a669a09cccbafc502f1", Name:="IDACI")>
    Public Function P073_IDACIAPriRate As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim APT_PP As Decimal = LAtoProv(Datasets.APTProformadataset.IDACIPrimaryBAAmountPerPupil)
        Dim AcadFilter As Decimal = F200_SBS_Academies
        If AcadFilter = 1 Then
            result = APT_PP
        Else
            exclude(rid)
        End if

        Print(APT_PP, "APT_PP", rid)
        Return result
    End Function

    <Calculation(Id:="c215b1f498a94ed683d2f3357e38771e")>
    <CalculationSpecification(Id:="P074_IDACIAPriSubtotal", Name:="P074_IDACIAPriSubtotal")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="86e2181721da4a669a09cccbafc502f1", Name:="IDACI")>
    Public Function P074_IDACIAPriSubtotal As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim SBS_P22 As Decimal = NOR_P22_Total_NOR_PRI_SBS
        Dim P073_IDACIAPriRate As Decimal = P073_IDACIAPriRate
        Dim P071_IDACIAPriFactor As Decimal = P071_IDACIAPriFactor
        Dim APT_ISB_IDACIA_Primary As Decimal = Datasets.APTNewISBdataset.IDACI(PA)
        Dim F100_AllAcademies As Decimal = F100_AllAcademies
        Dim AcadFilter As Decimal = F200_SBS_Academies
        Dim FundingBasis As decimal = F900_FundingBasis
        If AcadFilter = 1 then
            If F100_AllAcademies = 17181 Or (F100_AllAcademies = 17182 And FundingBasis = 2) Or (F100_AllAcademies = 17183 And FundingBasis = 2) Then
                Result = SBS_P22 * P073_IDACIAPriRate * P071_IDACIAPriFactor
            ElseIf F100_AllAcademies <> 16171 And FundingBasis = 1 Then
                result = APT_ISB_IDACIA_Primary
            Else
                Exclude(rid)
            End If
        else
            exclude(rid)
        End if

        Print(SBS_P22, "SBS_P22", rid)
        Print(P073_IDACIAPriRate, "P073_IDACIAPriRate", rid)
        Print(P071_IDACIAPriFactor, "P071_IDACIAPriFactor", rid)
        Print(APT_ISB_IDACIA_Primary, "APT_ISB_IDACIA_Primary", rid)
        Print(F100_AllAcademies, "F100_AllAcademies", rid)
        Print(FundingBasis, "Funding_Basis", rid)
        Return result
    End Function

    <Calculation(Id:="22e479c6d5b24210a9fffb0cce179992")>
    <CalculationSpecification(Id:="P075_NSENIDACIAPri", Name:="P075_NSENIDACIAPri")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="86e2181721da4a669a09cccbafc502f1", Name:="IDACI")>
    Public Function P075_NSENIDACIAPri As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim P074_IDACIAPriSubtotal As Decimal = P074_IDACIAPriSubtotal
        Dim P075a_NSENIDACIAPri_Percent As Decimal = P075a_NSENIDACIAPri_Percent / 100
        Dim AcadFilter As Decimal = F200_SBS_Academies
        If AcadFilter = 1 Then
            result = P074_IDACIAPriSubtotal * P075a_NSENIDACIAPri_Percent
        Else
            exclude(rid)
        End if

        Print(P074_IDACIAPriSubtotal, "P074_IDACIAPriSubtotal", rid)
        Print(P075a_NSENIDACIAPri_Percent, "P075a_NSENIDACIAPri_Percent", rid)
        Return result
    End Function

    <Calculation(Id:="6657206e824c4f18bb8c241e76959432")>
    <CalculationSpecification(Id:="P075a_NSENIDACIAPri_Percent", Name:="P075a_NSENIDACIAPri_Percent")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="86e2181721da4a669a09cccbafc502f1", Name:="IDACI")>
    Public Function P075a_NSENIDACIAPri_Percent As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim Notional_SEN As Decimal = LAtoProv(Datasets.APTProformadataset.IDACIBAPrimaryNotionalSEN)
        Dim AcadFilter As Decimal = F200_SBS_Academies
        If AcadFilter = 1 then
            result = Notional_SEN * 100
        Else
            exclude(rid)
        End if

        Print(Notional_SEN, "Notional_SEN", rid)
        Return result
    End Function

    <Calculation(Id:="8ce6d121e62f4f279f3149db9b54588f")>
    <CalculationSpecification(Id:="P076_InYearIDACIAPriSubtotal", Name:="P076_InYearIDACIAPriSubtotal")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="86e2181721da4a669a09cccbafc502f1", Name:="IDACI")>
    Public Function P076_InYearIDACIAPriSubtotal As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim P074_IDACIAPriSubtotal As Decimal = P074_IDACIAPriSubtotal
        Dim P001_1718DaysOpen As Decimal = P001_1718DaysOpen
        Dim Year_Days1718 As Decimal = P025_YearDays_1718
        Dim AcadFilter As Decimal = F200_SBS_Academies
        If AcadFilter = 1 Then
            result = P074_IDACIAPriSubtotal * (P001_1718DaysOpen / Year_Days1718)
        Else
            exclude(rid)
        End if

        Print(P074_IDACIAPriSubtotal, "P074_IDACIAPriSubtotal", rid)
        Print(P001_1718DaysOpen, "P001_1718DaysOpen", rid)
        Print(Year_Days1718, "Year_Days1718", rid)
        Return result
    End Function

    <Calculation(Id:="8e6e592433964d088d0c1b5e533d1549")>
    <CalculationSpecification(Id:="P077_IDACIFSecFactor", Name:="P077_IDACIFSecFactor")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="86e2181721da4a669a09cccbafc502f1", Name:="IDACI")>
    Public Function P077_IDACIFSecFactor As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim [Census_Pupil_Characteristics_IDACI_Secondary_Proportion_Band_F] As Decimal = Datasets.CensusPupilCharacteristics.IDACISecondaryProportionBandF
        Dim [APT_Inputs_and_Adjustments_IDACI_Secondary_Proportion_Band_F] As Decimal = Datasets.APTInputsandAdjustments.IDACISecondaryProportionBandF
        Dim [Local_Authority_Averages_IDACI_Secondary_Proportion_Band_F] As Decimal = LAtoProv(Datasets.LocalAuthorityAverages.IDACISecondaryProportionBandF)
        Dim IDACIFSecAdjString as string = Datasets.APTInputsandAdjustments.IDACISecondaryProportionBandF
        Dim IDACIFSecCensusString as string = Datasets.CensusPupilCharacteristics.IDACISecondaryProportionBandF
        Dim AcadFilter As Decimal = F200_SBS_Academies
        If AcadFilter = 1 Then
            If string.IsNullOrEmpty(IDACIFSecAdjString) And string.IsNullOrEmpty(IDACIFSecCensusString) Then
                result = [Local_Authority_Averages_IDACI_Secondary_Proportion_Band_F]
            Else
                If string.IsNullOrEmpty(IDACIFSecAdjString) Then
                    result = [Census_Pupil_Characteristics_IDACI_Secondary_Proportion_Band_F]
                Else
                    result = [APT_Inputs_and_Adjustments_IDACI_Secondary_Proportion_Band_F]
                End if
            End if
        Else
            exclude(rid)
        End if

        Print(Census_Pupil_Characteristics_IDACI_Secondary_Proportion_Band_F, "Census_Pupil_Characteristics_IDACI_Secondary_Proportion_Band_F", rid)
        Print(APT_Inputs_and_Adjustments_IDACI_Secondary_Proportion_Band_F, "APT_Inputs_and_Adjustments_IDACI_Secondary_Proportion_Band_F", rid)
        Print(Local_Authority_Averages_IDACI_Secondary_Proportion_Band_F, "Local_Authority_Averages_IDACI_Secondary_Proportion_Band_F", rid)
        Return result
    End Function

    <Calculation(Id:="70ec20c6ba104977969db687ec0a1d52")>
    <CalculationSpecification(Id:="P079_IDACIFSecRate", Name:="P079_IDACIFSecRate")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="86e2181721da4a669a09cccbafc502f1", Name:="IDACI")>
    Public Function P079_IDACIFSecRate As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim APT_PP As Decimal = LAtoProv(Datasets.APTProformadataset.IDACISecondaryBFAmountPerPupil)
        Dim AcadFilter As Decimal = F200_SBS_Academies
        If AcadFilter = 1 Then
            result = APT_PP
        Else
            exclude(rid)
        End if

        Print(APT_PP, "APT_PP", rid)
        Return result
    End Function

    <Calculation(Id:="4242d7e4a24f461c9f324cf80e7043e3")>
    <CalculationSpecification(Id:="P080_IDACIFSecSubtotal", Name:="P080_IDACIFSecSubtotal")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="86e2181721da4a669a09cccbafc502f1", Name:="IDACI")>
    Public Function P080_IDACIFSecSubtotal As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim SBS_P25 As Decimal = NOR_P25_Total_NOR_SEC_SBS
        Dim P079_IDACIFSecRate As Decimal = P079_IDACIFSecRate
        Dim P077_IDACIFSecFactor As Decimal = P077_IDACIFSecFactor
        Dim APT_ISB_IDACIF_Secondary As Decimal = Datasets.APTNewISBdataset.IDACI(SF)
        Dim F100_AllAcademies As Decimal = F100_AllAcademies
        Dim AcadFilter As Decimal = F200_SBS_Academies
        Dim FundingBasis As decimal = F900_FundingBasis
        If AcadFilter = 1 then
            If F100_AllAcademies = 17181 Or (F100_AllAcademies = 17182 And FundingBasis = 2) Or (F100_AllAcademies = 17183 And FundingBasis = 2) Then
                Result = SBS_P25 * P079_IDACIFSecRate * P077_IDACIFSecFactor
            ElseIf F100_AllAcademies <> 17181 And FundingBasis = 1 Then
                result = APT_ISB_IDACIF_Secondary
            Else
                Exclude(rid)
            End If
        else
            exclude(rid)
        End if

        Print(SBS_P25, "SBS_P25", rid)
        Print(P079_IDACIFSecRate, "P079_IDACIFSecRate", rid)
        Print(P077_IDACIFSecFactor, "P077_IDACIFSecFactor", rid)
        Print(APT_ISB_IDACIF_Secondary, "APT_ISB_IDACIF_Secondary", rid)
        Print(F100_AllAcademies, "F100_AllAcademies", rid)
        Print(FundingBasis, "Funding_Basis", rid)
        Return result
    End Function

    <Calculation(Id:="6334987a240f4bb7a69df02dddbf074f")>
    <CalculationSpecification(Id:="P081_NSENIDACIFSec", Name:="P081_NSENIDACIFSec")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="86e2181721da4a669a09cccbafc502f1", Name:="IDACI")>
    Public Function P081_NSENIDACIFSec As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim P080_IDACIFSecSubtotal As Decimal = P080_IDACIFSecSubtotal
        Dim P081a_NSENIDACIFSec_Percent As Decimal = P081a_NSENIDACIFSec_Percent / 100
        Dim AcadFilter As Decimal = F200_SBS_Academies
        If AcadFilter = 1 Then
            result = P080_IDACIFSecSubtotal * P081a_NSENIDACIFSec_Percent
        Else
            exclude(rid)
        End if

        Print(P080_IDACIFSecSubtotal, "P080_IDACIFSecSubtotal", rid)
        Print(P081a_NSENIDACIFSec_Percent, "P081a_NSENIDACIFSec_Percent", rid)
        Return result
    End Function

    <Calculation(Id:="556b159544f94f5c9bca7d37550fa93b")>
    <CalculationSpecification(Id:="P081a_NSENIDACIFSec_Percent", Name:="P081a_NSENIDACIFSec_Percent")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="86e2181721da4a669a09cccbafc502f1", Name:="IDACI")>
    Public Function P081a_NSENIDACIFSec_Percent As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim Notional_SEN As Decimal = LAtoProv(Datasets.APTProformadataset.IDACIBFSecondaryNotionalSEN)
        Dim AcadFilter As Decimal = F200_SBS_Academies
        If AcadFilter = 1 then
            result = Notional_SEN * 100
        Else
            exclude(rid)
        End if

        Print(Notional_SEN, "Notional_SEN", rid)
        Return result
    End Function

    <Calculation(Id:="bc90ad44732641a0af986ef3dd4c650b")>
    <CalculationSpecification(Id:="P082_InYearIDACIFSecSubtotal", Name:="P082_InYearIDACIFSecSubtotal")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="86e2181721da4a669a09cccbafc502f1", Name:="IDACI")>
    Public Function P082_InYearIDACIFSecSubtotal As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim P080_IDACIFSecSubtotal As Decimal = P080_IDACIFSecSubtotal
        Dim P001_1718DaysOpen As Decimal = P001_1718DaysOpen
        Dim Year_Days1718 As Decimal = P025_YearDays_1718
        Dim AcadFilter As Decimal = F200_SBS_Academies
        If AcadFilter = 1 Then
            result = P080_IDACIFSecSubtotal * (P001_1718DaysOpen / Year_Days1718)
        Else
            exclude(rid)
        End if

        Print(P080_IDACIFSecSubtotal, "P080_IDACIFSecSubtotal", rid)
        Print(P001_1718DaysOpen, "P001_1718DaysOpen", rid)
        Print(Year_Days1718, "Year_Days1718", rid)
        Return result
    End Function

    <Calculation(Id:="62fbf653867d4acca555511864d89ece")>
    <CalculationSpecification(Id:="P083_IDACIESecFactor", Name:="P083_IDACIESecFactor")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="86e2181721da4a669a09cccbafc502f1", Name:="IDACI")>
    Public Function P083_IDACIESecFactor As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim [Census_Pupil_Characteristics_IDACI_Secondary_Proportion_Band_E] As Decimal = Datasets.CensusPupilCharacteristics.IDACISecondaryProportionBandE
        Dim [APT_Inputs_and_Adjustments_IDACI_Secondary_Proportion_Band_E] As Decimal = Datasets.APTInputsandAdjustments.IDACISecondaryProportionBandE
        Dim [Local_Authority_Averages_IDACI_Secondary_Proportion_Band_E] As Decimal = LAtoProv(Datasets.LocalAuthorityAverages.IDACISecondaryProportionBandE)
        Dim IDACIESecAdjString as string = Datasets.APTInputsandAdjustments.IDACISecondaryProportionBandE
        Dim IDACIESecCensusString as string = Datasets.CensusPupilCharacteristics.IDACISecondaryProportionBandE
        Dim AcadFilter As Decimal = F200_SBS_Academies
        If AcadFilter = 1 Then
            If string.IsNullOrEmpty(IDACIESecAdjString) And string.IsNullOrEmpty(IDACIESecCensusString) Then
                result = [Local_Authority_Averages_IDACI_Secondary_Proportion_Band_E]
            Else
                If string.IsNullOrEmpty(IDACIESecAdjString) Then
                    result = [Census_Pupil_Characteristics_IDACI_Secondary_Proportion_Band_E]
                Else
                    result = [APT_Inputs_and_Adjustments_IDACI_Secondary_Proportion_Band_E]
                End if
            End if
        Else
            exclude(rid)
        End if

        Print(Census_Pupil_Characteristics_IDACI_Secondary_Proportion_Band_E, "Census_Pupil_Characteristics_IDACI_Secondary_Proportion_Band_E", rid)
        Print(APT_Inputs_and_Adjustments_IDACI_Secondary_Proportion_Band_E, "APT_Inputs_and_Adjustments_IDACI_Secondary_Proportion_Band_E", rid)
        Print(Local_Authority_Averages_IDACI_Secondary_Proportion_Band_E, "Local_Authority_Averages_IDACI_Secondary_Proportion_Band_E", rid)
        Return result
    End Function

    <Calculation(Id:="081edc6b605f45368995b979f954dd79")>
    <CalculationSpecification(Id:="P085_IDACIESecRate", Name:="P085_IDACIESecRate")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="86e2181721da4a669a09cccbafc502f1", Name:="IDACI")>
    Public Function P085_IDACIESecRate As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim APT_PP As Decimal = LAtoProv(Datasets.APTProformadataset.IDACISecondaryBEAmountPerPupil)
        Dim AcadFilter As Decimal = F200_SBS_Academies
        If AcadFilter = 1 Then
            result = APT_PP
        Else
            exclude(rid)
        End if

        Print(APT_PP, "APT_PP", rid)
        Return result
    End Function

    <Calculation(Id:="dcab7731347a4d70834c0c22369ba940")>
    <CalculationSpecification(Id:="P086_IDACIESecSubtotal", Name:="P086_IDACIESecSubtotal")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="86e2181721da4a669a09cccbafc502f1", Name:="IDACI")>
    Public Function P086_IDACIESecSubtotal As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim SBS_P25 As Decimal = NOR_P25_Total_NOR_SEC_SBS
        Dim P085_IDACIESecRate As Decimal = P085_IDACIESecRate
        Dim P083_IDACIESecFactor As Decimal = P083_IDACIESecFactor
        Dim APT_ISB_IDACIE_Secondary As Decimal = Datasets.APTNewISBdataset.IDACI(SE)
        Dim F100_AllAcademies As Decimal = F100_AllAcademies
        Dim AcadFilter As Decimal = F200_SBS_Academies
        Dim FundingBasis As decimal = F900_FundingBasis
        If AcadFilter = 1 then
            If F100_AllAcademies = 17181 Or (F100_AllAcademies = 17182 And FundingBasis = 2) Or (F100_AllAcademies = 17183 And FundingBasis = 2) Then
                Result = SBS_P25 * P085_IDACIESecRate * P083_IDACIESecFactor
            ElseIf F100_AllAcademies <> 17181 And FundingBasis = 2 Then
                result = APT_ISB_IDACIE_Secondary
            Else
                Exclude(rid)
            End If
        else
            exclude(rid)
        End if

        Print(SBS_P25, "SBS_P25", rid)
        Print(P085_IDACIESecRate, "P085_IDACIESecRate", rid)
        Print(P083_IDACIESecFactor, "P083_IDACIESecFactor", rid)
        Print(APT_ISB_IDACIE_Secondary, "APT_ISB_IDACIE_Secondary", rid)
        Print(F100_AllAcademies, "F100_AllAcademies", rid)
        Print(FundingBasis, "Funding_Basis", rid)
        Return result
    End Function

    <Calculation(Id:="9dd47ba9ec314b1f9fb726ebc3b2c687")>
    <CalculationSpecification(Id:="P087_NSENIDACIESec", Name:="P087_NSENIDACIESec")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="86e2181721da4a669a09cccbafc502f1", Name:="IDACI")>
    Public Function P087_NSENIDACIESec As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim P086_IDACIESecSubtotal As Decimal = P086_IDACIESecSubtotal
        Dim P087a_NSENIDACIESec_Percent As Decimal = P087a_NSENIDACIESec_Percent / 100
        Dim AcadFilter As Decimal = F200_SBS_Academies
        If AcadFilter = 1 Then
            result = P086_IDACIESecSubtotal * P087a_NSENIDACIESec_Percent
        Else
            exclude(rid)
        End if

        Print(P086_IDACIESecSubtotal, "P086_IDACIESecSubtotal", rid)
        Print(P087a_NSENIDACIESec_Percent, "P087a_NSENIDACIESec_Percent", rid)
        Return result
    End Function

    <Calculation(Id:="0650c4adf32a4d47ac3dfc07ef76899a")>
    <CalculationSpecification(Id:="P87a_NSENIDACIESec_Percent", Name:="P87a_NSENIDACIESec_Percent")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="86e2181721da4a669a09cccbafc502f1", Name:="IDACI")>
    Public Function P87a_NSENIDACIESec_Percent As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="f0a60cf1ba1d458d8f7df3f42d4c69e7")>
    <CalculationSpecification(Id:="P088_InYearIDACIESecSubtotal", Name:="P088_InYearIDACIESecSubtotal")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="86e2181721da4a669a09cccbafc502f1", Name:="IDACI")>
    Public Function P088_InYearIDACIESecSubtotal As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim P086_IDACIESecSubtotal As Decimal = P086_IDACIESecSubtotal
        Dim P001_1718DaysOpen As Decimal = P001_1718DaysOpen
        Dim Year_Days1718 As Decimal = P025_YearDays_1718
        Dim AcadFilter As Decimal = F200_SBS_Academies
        If AcadFilter = 1 Then
            result = P086_IDACIESecSubtotal * (P001_1718DaysOpen / Year_Days1718)
        Else
            exclude(rid)
        End if

        Print(P086_IDACIESecSubtotal, "P086_IDACIESecSubtotal", rid)
        Print(P001_1718DaysOpen, "P001_1718DaysOpen", rid)
        Print(Year_Days1718, "Year_Days1718", rid)
        Return result
    End Function

    <Calculation(Id:="b9ea4e1799b441f8b148e586cd5d067d")>
    <CalculationSpecification(Id:="P089_IDACIDSecFactor", Name:="P089_IDACIDSecFactor")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="86e2181721da4a669a09cccbafc502f1", Name:="IDACI")>
    Public Function P089_IDACIDSecFactor As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim [Census_Pupil_Characteristics_IDACI_Secondary_Proportion_Band_D] As Decimal = Datasets.CensusPupilCharacteristics.IDACISecondaryProportionBandD
        Dim [APT_Inputs_and_Adjustments_IDACI_Secondary_Proportion_Band_D] As Decimal = Datasets.APTInputsandAdjustments.IDACISecondaryProportionBandD
        Dim [Local_Authority_Averages_IDACI_Secondary_Proportion_Band_D] As Decimal = LAtoProv(Datasets.LocalAuthorityAverages.IDACISecondaryProportionBandD)
        Dim IDACIDSecAdjString as string = Datasets.APTInputsandAdjustments.IDACISecondaryProportionBandD
        Dim IDACIDSecCensusString as string = Datasets.CensusPupilCharacteristics.IDACISecondaryProportionBandD
        Dim AcadFilter As Decimal = F200_SBS_Academies
        If AcadFilter = 1 Then
            If string.IsNullOrEmpty(IDACIDSecAdjString) And string.IsNullOrEmpty(IDACIDSecCensusString) Then
                result = [Local_Authority_Averages_IDACI_Secondary_Proportion_Band_D]
            Else
                If string.IsNullOrEmpty(IDACIDSecAdjString) Then
                    result = [Census_Pupil_Characteristics_IDACI_Secondary_Proportion_Band_D]
                Else
                    result = [APT_Inputs_and_Adjustments_IDACI_Secondary_Proportion_Band_D]
                End if
            End if
        Else
            exclude(rid)
        End if

        Print(Census_Pupil_Characteristics_IDACI_Secondary_Proportion_Band_D, "Census_Pupil_Characteristics_IDACI_Secondary_Proportion_Band_D", rid)
        Print(APT_Inputs_and_Adjustments_IDACI_Secondary_Proportion_Band_D, "APT_Inputs_and_Adjustments_IDACI_Secondary_Proportion_Band_D", rid)
        Print(Local_Authority_Averages_IDACI_Secondary_Proportion_Band_D, "Local_Authority_Averages_IDACI_Secondary_Proportion_Band_D", rid)
        Return result
    End Function

    <Calculation(Id:="97ed8586b34149da848ac01071f39b8c")>
    <CalculationSpecification(Id:="P091_IDACIDSecRate", Name:="P091_IDACIDSecRate")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="86e2181721da4a669a09cccbafc502f1", Name:="IDACI")>
    Public Function P091_IDACIDSecRate As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim APT_PP As Decimal = LAtoProv(Datasets.APTProformadataset.IDACISecondaryBDAmountPerPupil)
        Dim AcadFilter As Decimal = F200_SBS_Academies
        If AcadFilter = 1 Then
            result = APT_PP
        Else
            exclude(rid)
        End if

        Print(APT_PP, "APT_PP", rid)
        Return result
    End Function

    <Calculation(Id:="e5a044b537bc4be9a4d27379ebf75623")>
    <CalculationSpecification(Id:="P092_IDACIDSecSubtotal", Name:="P092_IDACIDSecSubtotal")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="86e2181721da4a669a09cccbafc502f1", Name:="IDACI")>
    Public Function P092_IDACIDSecSubtotal As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim SBS_P25 As Decimal = NOR_P25_Total_NOR_SEC_SBS
        Dim P091_IDACIDSecRate As Decimal = P091_IDACIDSecRate
        Dim P089_IDACIDSecFactor As Decimal = P089_IDACIDSecFactor
        Dim APT_ISB_IDACID_Secondary As Decimal = Datasets.APTNewISBdataset.IDACI(SD)
        Dim F100_AllAcademies As Decimal = F100_AllAcademies
        Dim AcadFilter As Decimal = F200_SBS_Academies
        Dim FundingBasis As decimal = F900_FundingBasis
        If AcadFilter = 1 then
            If F100_AllAcademies = 17181 Or (F100_AllAcademies = 17182 And FundingBasis = 2) Or (F100_AllAcademies = 17183 And FundingBasis = 2) Then
                Result = SBS_P25 * P091_IDACIDSecRate * P089_IDACIDSecFactor
            ElseIf F100_AllAcademies <> 17181 And FundingBasis = 1 Then
                result = APT_ISB_IDACID_Secondary
            Else
                Exclude(rid)
            End If
        else
            exclude(rid)
        End If

        Print(SBS_P25, "SBS_P25", rid)
        Print(P091_IDACIDSecRate, "P091_IDACIDSecRate", rid)
        Print(P089_IDACIDSecFactor, "P089_IDACIDSecFactor", rid)
        Print(APT_ISB_IDACID_Secondary, "APT_ISB_IDACID_Secondary", rid)
        Print(F100_AllAcademies, "F100_AllAcademies", rid)
        Print(FundingBasis, "Funding_Basis", rid)
        Return result
    End Function

    <Calculation(Id:="ac9bd5b143684baf89d733e87f355f45")>
    <CalculationSpecification(Id:="P093_NSENIDACIDSec", Name:="P093_NSENIDACIDSec")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="86e2181721da4a669a09cccbafc502f1", Name:="IDACI")>
    Public Function P093_NSENIDACIDSec As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim P092_IDACIDSecSubtotal As Decimal = P092_IDACIDSecSubtotal
        Dim P093a_NSENIDACIDSec_Percent As Decimal = P093a_NSENIDACIDSec_Percent / 100
        Dim AcadFilter As Decimal = F200_SBS_Academies
        If AcadFilter = 1 Then
            result = P092_IDACIDSecSubtotal * P093a_NSENIDACIDSec_Percent
        Else
            exclude(rid)
        End if

        Print(P092_IDACIDSecSubtotal, "P092_IDACIDSecSubtotal", rid)
        Print(P093a_NSENIDACIDSec_Percent, "P093a_NSENIDACIDSec_Percent", rid)
        Return result
    End Function

    <Calculation(Id:="206fb0ef38dc4a08a4299def3c2a7580")>
    <CalculationSpecification(Id:="P093a_NSENIDACIDSec_Percent", Name:="P093a_NSENIDACIDSec_Percent")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="86e2181721da4a669a09cccbafc502f1", Name:="IDACI")>
    Public Function P093a_NSENIDACIDSec_Percent As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim Notional_SEN As Decimal = LAtoProv(Datasets.APTProformadataset.IDACIBDSecondaryNotionalSEN)
        Dim AcadFilter As Decimal = F200_SBS_Academies
        If AcadFilter = 1 then
            result = Notional_SEN * 100
        Else
            exclude(rid)
        End if

        Print(Notional_SEN, "Notional_SEN", rid)
        Return result
    End Function

    <Calculation(Id:="59956adc5a73470dbcdbeb2193fd7f80")>
    <CalculationSpecification(Id:="P094_InYearIDACIDSecSubtotal", Name:="P094_InYearIDACIDSecSubtotal")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="86e2181721da4a669a09cccbafc502f1", Name:="IDACI")>
    Public Function P094_InYearIDACIDSecSubtotal As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim P092_IDACIDSecSubtotal As Decimal = P092_IDACIDSecSubtotal
        Dim P001_1718DaysOpen As Decimal = P001_1718DaysOpen
        Dim Year_Days1718 As Decimal = P025_YearDays_1718
        Dim AcadFilter As Decimal = F200_SBS_Academies
        If AcadFilter = 1 Then
            result = P092_IDACIDSecSubtotal * (P001_1718DaysOpen / Year_Days1718)
        Else
            exclude(rid)
        End if

        Print(P092_IDACIDSecSubtotal, "P092_IDACIDSecSubtotal", rid)
        Print(P001_1718DaysOpen, "P001_1718DaysOpen", rid)
        Print(Year_Days1718, "Year_Days1718", rid)
        Return result
    End Function

    <Calculation(Id:="2b0a22b457f944d5b4302802df607dde")>
    <CalculationSpecification(Id:="P095_IDACICSecFactor", Name:="P095_IDACICSecFactor")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="86e2181721da4a669a09cccbafc502f1", Name:="IDACI")>
    Public Function P095_IDACICSecFactor As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim [Census_Pupil_Characteristics_IDACI_Secondary_Proportion_Band_C] As Decimal = Datasets.CensusPupilCharacteristics.IDACISecondaryProportionBandC
        Dim [APT_Inputs_and_Adjustments_IDACI_Secondary_Proportion_Band_C] As Decimal = Datasets.APTInputsandAdjustments.IDACISecondaryProportionBandC
        Dim [Local_Authority_Averages_IDACI_Secondary_Proportion_Band_C] As Decimal = LAtoProv(Datasets.LocalAuthorityAverages.IDACISecondaryProportionBandC)
        Dim IDACICSecAdjString as string = Datasets.APTInputsandAdjustments.IDACISecondaryProportionBandC
        Dim IDACICSecCensusString as string = Datasets.CensusPupilCharacteristics.IDACISecondaryProportionBandC
        Dim AcadFilter As Decimal = F200_SBS_Academies
        If AcadFilter = 1 Then
            If string.IsNullOrEmpty(IDACICSecAdjString) And string.IsNullOrEmpty(IDACICSecCensusString) Then
                result = [Local_Authority_Averages_IDACI_Secondary_Proportion_Band_C]
            Else
                If string.IsNullOrEmpty(IDACICSecAdjString) Then
                    result = [Census_Pupil_Characteristics_IDACI_Secondary_Proportion_Band_C]
                Else
                    result = [APT_Inputs_and_Adjustments_IDACI_Secondary_Proportion_Band_C]
                End if
            End if
        Else
            exclude(rid)
        End if

        Print(Census_Pupil_Characteristics_IDACI_Secondary_Proportion_Band_C, "Census_Pupil_Characteristics_IDACI_Secondary_Proportion_Band_C", rid)
        Print(APT_Inputs_and_Adjustments_IDACI_Secondary_Proportion_Band_C, "APT_Inputs_and_Adjustments_IDACI_Secondary_Proportion_Band_C", rid)
        Print(Local_Authority_Averages_IDACI_Secondary_Proportion_Band_C, "Local_Authority_Averages_IDACI_Secondary_Proportion_Band_C", rid)
        Return result
    End Function

    <Calculation(Id:="5c11c1fc79c84be3be4663eee4151b00")>
    <CalculationSpecification(Id:="P097_IDACICSecRate", Name:="P097_IDACICSecRate")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="86e2181721da4a669a09cccbafc502f1", Name:="IDACI")>
    Public Function P097_IDACICSecRate As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim APT_PP As Decimal = LAtoProv(Datasets.APTProformadataset.IDACISecondaryBCAmountPerPupil)
        Dim AcadFilter As Decimal = F200_SBS_Academies
        If AcadFilter = 1 Then
            result = APT_PP
        Else
            exclude(rid)
        End if

        Print(APT_PP, "APT_PP", rid)
        Return result
    End Function

    <Calculation(Id:="f50deb5b1f594e4481a67d7085bde7db")>
    <CalculationSpecification(Id:="P098_IDACICSecSubtotal", Name:="P098_IDACICSecSubtotal")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="86e2181721da4a669a09cccbafc502f1", Name:="IDACI")>
    Public Function P098_IDACICSecSubtotal As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim SBS_P25 As Decimal = NOR_P25_Total_NOR_SEC_SBS
        Dim P097_IDACICSecRate As Decimal = P097_IDACICSecRate
        Dim P095_IDACICSecFactor As Decimal = P095_IDACICSecFactor
        Dim APT_ISB_IDACIC_Secondary As Decimal = Datasets.APTNewISBdataset.IDACI(SC)
        Dim F100_AllAcademies As Decimal = F100_AllAcademies
        Dim AcadFilter As Decimal = F200_SBS_Academies
        Dim FundingBasis As Decimal = F900_FundingBasis
        If AcadFilter = 1 then
            If F100_AllAcademies = 17181 Or (F100_AllAcademies = 17182 And FundingBasis = 2) Or (F100_AllAcademies = 17183 And FundingBasis = 2) Then
                Result = SBS_P25 * P097_IDACICSecRate * P095_IDACICSecFactor
            ElseIf F100_AllAcademies <> 16171 And FundingBasis = 1 Then
                result = APT_ISB_IDACIC_Secondary
            Else
                Exclude(rid)
            End If
        else
            exclude(rid)
        End If

        Print(SBS_P25, "SBS_P25", rid)
        Print(P097_IDACICSecRate, "P097_IDACICSecRate", rid)
        Print(P095_IDACICSecFactor, "P095_IDACICSecFactor", rid)
        Print(APT_ISB_IDACIC_Secondary, "APT_ISB_IDACIC_Secondary", rid)
        Print(F100_AllAcademies, "F100_AllAcademies", rid)
        Print(FundingBasis, "Funding_Basis", rid)
        Return result
    End Function

    <Calculation(Id:="fdbd7f8c364949e7a7186c814de9169e")>
    <CalculationSpecification(Id:="P099_NSENIDACICSec", Name:="P099_NSENIDACICSec")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="86e2181721da4a669a09cccbafc502f1", Name:="IDACI")>
    Public Function P099_NSENIDACICSec As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim P098_IDACICSecSubtotal As Decimal = P098_IDACICSecSubtotal
        Dim P099a_NSENIDACICSec_Percent As Decimal = P099a_NSENIDACISec_Percent / 100
        Dim AcadFilter As Decimal = F200_SBS_Academies
        If AcadFilter = 1 Then
            result = P098_IDACICSecSubtotal * P099a_NSENIDACICSec_Percent
        Else
            exclude(rid)
        End if

        Print(P098_IDACICSecSubtotal, "P098_IDACICSecSubtotal", rid)
        Print(P099a_NSENIDACICSec_Percent, "P099a_NSENIDACICSec_Percent", rid)
        Return result
    End Function

    <Calculation(Id:="e4c726dc90f94c25b719c52c96890427")>
    <CalculationSpecification(Id:="P099a_NSENIDACICSec_Percent", Name:="P099a_NSENIDACICSec_Percent")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="86e2181721da4a669a09cccbafc502f1", Name:="IDACI")>
    Public Function P099a_NSENIDACICSec_Percent As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="90ca9129bf214b53af0cceb3e373f13d")>
    <CalculationSpecification(Id:="P100_InYearIDACICSecSubtotal", Name:="P100_InYearIDACICSecSubtotal")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="86e2181721da4a669a09cccbafc502f1", Name:="IDACI")>
    Public Function P100_InYearIDACICSecSubtotal As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim P098_IDACICSecSubtotal As Decimal = P098_IDACICSecSubtotal
        Dim P001_1718DaysOpen As Decimal = P001_1718DaysOpen
        Dim Year_Days1718 As Decimal = P025_YearDays_1718
        Dim AcadFilter As Decimal = F200_SBS_Academies
        If AcadFilter = 1 Then
            result = P098_IDACICSecSubtotal * (P001_1718DaysOpen / Year_Days1718)
        Else
            exclude(rid)
        End if

        Print(P098_IDACICSecSubtotal, "P098_IDACICSecSubtotal", rid)
        Print(P001_1718DaysOpen, "P001_1718DaysOpen", rid)
        Print(Year_Days1718, "Year_Days1718", rid)
        Return result
    End Function

    <Calculation(Id:="fa0540c8433f4527b02bfe72850d67a3")>
    <CalculationSpecification(Id:="P101_IDACIBSecFactor", Name:="P101_IDACIBSecFactor")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="86e2181721da4a669a09cccbafc502f1", Name:="IDACI")>
    Public Function P101_IDACIBSecFactor As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim [Census_Pupil_Characteristics_IDACI_Secondary_Proportion_Band_B] As Decimal = Datasets.CensusPupilCharacteristics.IDACISecondaryProportionBandB
        Dim [APT_Inputs_and_Adjustments_IDACI_Secondary_Proportion_Band_B] As Decimal = Datasets.APTInputsandAdjustments.IDACISecondaryProportionBandB
        Dim [Local_Authority_Averages_IDACI_Secondary_Proportion_Band_B] As Decimal = LAtoProv(Datasets.LocalAuthorityAverages.IDACISecondaryProportionBandB)
        Dim IDACIBSecAdjString as string = Datasets.APTInputsandAdjustments.IDACISecondaryProportionBandB
        Dim IDACIBSecCensusString as string = Datasets.CensusPupilCharacteristics.IDACISecondaryProportionBandB
        Dim AcadFilter As Decimal = F200_SBS_Academies
        If AcadFilter = 1 Then
            If string.IsNullOrEmpty(IDACIBSecAdjString) And string.IsNullOrEmpty(IDACIBSecCensusString) Then
                result = [Local_Authority_Averages_IDACI_Secondary_Proportion_Band_B]
            Else
                If string.IsNullOrEmpty(IDACIBSecAdjString) Then
                    result = [Census_Pupil_Characteristics_IDACI_Secondary_Proportion_Band_B]
                Else
                    result = [APT_Inputs_and_Adjustments_IDACI_Secondary_Proportion_Band_B]
                End if
            End if
        Else
            exclude(rid)
        End if

        Print(Census_Pupil_Characteristics_IDACI_Secondary_Proportion_Band_B, "Census_Pupil_Characteristics_IDACI_Secondary_Proportion_Band_B", rid)
        Print(APT_Inputs_and_Adjustments_IDACI_Secondary_Proportion_Band_B, "APT_Inputs_and_Adjustments_IDACI_Secondary_Proportion_Band_B", rid)
        Print(Local_Authority_Averages_IDACI_Secondary_Proportion_Band_B, "Local_Authority_Averages_IDACI_Secondary_Proportion_Band_B", rid)
        Return result
    End Function

    <Calculation(Id:="79882f324ffe433eb2b0525de291bc17")>
    <CalculationSpecification(Id:="P103_IDACIBSecRate", Name:="P103_IDACIBSecRate")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="86e2181721da4a669a09cccbafc502f1", Name:="IDACI")>
    Public Function P103_IDACIBSecRate As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim APT_PP As Decimal = LAtoProv(Datasets.APTProformadataset.IDACISecondaryBBAmountPerPupil)
        Dim AcadFilter As Decimal = F200_SBS_Academies
        If AcadFilter = 1 Then
            result = APT_PP
        Else
            exclude(rid)
        End if

        Print(APT_PP, "APT_PP", rid)
        Return result
    End Function

    <Calculation(Id:="5e958f6f5e854efdb3b4b9ef796ef94a")>
    <CalculationSpecification(Id:="P104_IDACIBSecSubtotal", Name:="P104_IDACIBSecSubtotal")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="86e2181721da4a669a09cccbafc502f1", Name:="IDACI")>
    Public Function P104_IDACIBSecSubtotal As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim SBS_P25 As Decimal = NOR_P25_Total_NOR_SEC_SBS
        Dim P103_IDACIBSecRate As Decimal = P103_IDACIBSecRate
        Dim P101_IDACIBSecFactor As Decimal = P101_IDACIBSecFactor
        Dim APT_ISB_IDACIB_Secondary As Decimal = Datasets.APTNewISBdataset.IDACI(SB)
        Dim F100_AllAcademies As Decimal = F100_AllAcademies
        Dim AcadFilter As Decimal = F200_SBS_Academies
        Dim FundingBasis As decimal = F900_FundingBasis
        If AcadFilter = 1 then
            If F100_AllAcademies = 17181 Or (F100_AllAcademies = 17182 And FundingBasis = 2) Or (F100_AllAcademies = 17183 And FundingBasis = 2) Then
                Result = SBS_P25 * P103_IDACIBSecRate * P101_IDACIBSecFactor
            ElseIf F100_AllAcademies <> 17181 And FundingBasis = 1 Then
                result = APT_ISB_IDACIB_Secondary
            Else
                Exclude(rid)
            End If
        else
            exclude(rid)
        End if

        Print(SBS_P25, "SBS_P25", rid)
        Print(P103_IDACIBSecRate, "P103_IDACIBSecRate", rid)
        Print(P101_IDACIBSecFactor, "P101_IDACIBSecFactor", rid)
        Print(APT_ISB_IDACIB_Secondary, "APT_ISB_IDACIB_Secondary", rid)
        Print(F100_AllAcademies, "F100_AllAcademies", rid)
        Print(FundingBasis, "Funding_Basis", rid)
        Return result
    End Function

    <Calculation(Id:="81c6d19663bc4e12a52cadbed657ccbc")>
    <CalculationSpecification(Id:="P105_NSENIDACIBSec", Name:="P105_NSENIDACIBSec")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="86e2181721da4a669a09cccbafc502f1", Name:="IDACI")>
    Public Function P105_NSENIDACIBSec As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim P104_IDACIBSecSubtotal As Decimal = P104_IDACIBSecSubtotal
        Dim P105a_NSENIDACIBSec_Percent As Decimal = P105a_NSENIDACIBSec_Percent / 100
        Dim AcadFilter As Decimal = F200_SBS_Academies
        If AcadFilter = 1 Then
            result = P104_IDACIBSecSubtotal * P105a_NSENIDACIBSec_Percent
        Else
            exclude(rid)
        End if

        Print(P104_IDACIBSecSubtotal, "P104_IDACIBSecSubtotal", rid)
        Print(P105a_NSENIDACIBSec_Percent, "P105a_NSENIDACIBSec_Percent", rid)
        Return result
    End Function

    <Calculation(Id:="ed7c46941e6d466f9075c163433462d4")>
    <CalculationSpecification(Id:="P105a_NSENIDACIBSec_Percent", Name:="P105a_NSENIDACIBSec_Percent")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="86e2181721da4a669a09cccbafc502f1", Name:="IDACI")>
    Public Function P105a_NSENIDACIBSec_Percent As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim Notional_SEN As Decimal = LAtoProv(Datasets.APTProformadataset.IDACIBBSecondaryNotionalSEN)
        Dim AcadFilter As Decimal = F200_SBS_Academies
        If AcadFilter = 1 then
            result = Notional_SEN * 100
        Else
            exclude(rid)
        End if

        Print(Notional_SEN, "Notional_SEN", rid)
        Return result
    End Function

    <Calculation(Id:="71d934d70d3e473386290ad2e08e3759")>
    <CalculationSpecification(Id:="P106_InYearIDACIBSecSubtotal", Name:="P106_InYearIDACIBSecSubtotal")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="86e2181721da4a669a09cccbafc502f1", Name:="IDACI")>
    Public Function P106_InYearIDACIBSecSubtotal As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim P104_IDACIBSecSubtotal As Decimal = P104_IDACIBSecSubtotal
        Dim P001_1718DaysOpen As Decimal = P001_1718DaysOpen
        Dim Year_Days1718 As Decimal = P025_YearDays_1718
        Dim AcadFilter As Decimal = F200_SBS_Academies
        If AcadFilter = 1 Then
            result = P104_IDACIBSecSubtotal * (P001_1718DaysOpen / Year_Days1718)
        Else
            exclude(rid)
        End if

        Print(P104_IDACIBSecSubtotal, "P104_IDACIBSecSubtotal", rid)
        Print(P001_1718DaysOpen, "P001_1718DaysOpen", rid)
        Print(Year_Days1718, "Year_Days1718", rid)
        Return result
    End Function

    <Calculation(Id:="145c7dac6e3347558d2ed30298b6afec")>
    <CalculationSpecification(Id:="P107_IDACIASecFactor", Name:="P107_IDACIASecFactor")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="86e2181721da4a669a09cccbafc502f1", Name:="IDACI")>
    Public Function P107_IDACIASecFactor As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim [Census_Pupil_Characteristics_IDACI_Secondary_Proportion_Band_A] As Decimal = Datasets.CensusPupilCharacteristics.IDACISecondaryProportionBandA
        Dim [APT_Inputs_and_Adjustments_IDACI_Secondary_Proportion_Band_A] As Decimal = Datasets.APTInputsandAdjustments.IDACISecondaryProportionBandA
        Dim [Local_Authority_Averages_IDACI_Secondary_Proportion_Band_A] As Decimal = LAtoProv(Datasets.LocalAuthorityAverages.IDACISecondaryProportionBandA)
        Dim IDACIASecAdjString as string = Datasets.APTInputsandAdjustments.IDACISecondaryProportionBandA
        Dim IDACIASecCensusString as string = Datasets.CensusPupilCharacteristics.IDACISecondaryProportionBandA
        Dim AcadFilter As Decimal = F200_SBS_Academies
        If AcadFilter = 1 Then
            If string.IsNullOrEmpty(IDACIASecAdjString) And string.IsNullOrEmpty(IDACIASecCensusString) Then
                result = [Local_Authority_Averages_IDACI_Secondary_Proportion_Band_A]
            Else
                If string.IsNullOrEmpty(IDACIASecAdjString) Then
                    result = [Census_Pupil_Characteristics_IDACI_Secondary_Proportion_Band_A]
                Else
                    result = [APT_Inputs_and_Adjustments_IDACI_Secondary_Proportion_Band_A]
                End if
            End if
        Else
            exclude(rid)
        End if

        Print(Census_Pupil_Characteristics_IDACI_Secondary_Proportion_Band_A, "Census_Pupil_Characteristics_IDACI_Secondary_Proportion_Band_A", rid)
        Print(APT_Inputs_and_Adjustments_IDACI_Secondary_Proportion_Band_A, "APT_Inputs_and_Adjustments_IDACI_Secondary_Proportion_Band_A", rid)
        Print(Local_Authority_Averages_IDACI_Secondary_Proportion_Band_A, "Local_Authority_Averages_IDACI_Secondary_Proportion_Band_A", rid)
        Return result
    End Function

    <Calculation(Id:="38b95cd95ceb43da940cf377c8ace5e4")>
    <CalculationSpecification(Id:="P109_IDACIASecRate", Name:="P109_IDACIASecRate")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="86e2181721da4a669a09cccbafc502f1", Name:="IDACI")>
    Public Function P109_IDACIASecRate As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim APT_PP As Decimal = LAtoProv(Datasets.APTProformadataset.IDACISecondaryBAAmountPerPupil)
        Dim AcadFilter As Decimal = F200_SBS_Academies
        If AcadFilter = 1 Then
            result = APT_PP
        Else
            exclude(rid)
        End if

        Print(APT_PP, "APT_PP", rid)
        Return result
    End Function

    <Calculation(Id:="632c4b7d4428481d897486540f6d0ec6")>
    <CalculationSpecification(Id:="P110_IDACIASecSubtotal", Name:="P110_IDACIASecSubtotal")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="86e2181721da4a669a09cccbafc502f1", Name:="IDACI")>
    Public Function P110_IDACIASecSubtotal As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="bfee28185e414cc383d32a6fbfbe6e19")>
    <CalculationSpecification(Id:="P111_NSENIDACIASec", Name:="P111_NSENIDACIASec")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="86e2181721da4a669a09cccbafc502f1", Name:="IDACI")>
    Public Function P111_NSENIDACIASec As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim P110_IDACIASecSubtotal As Decimal = P110_IDACIASecSubtotal
        Dim P111a_NSENIDACIASec_Percent As Decimal = P111a_NSENIDACIASec_Percent / 100
        Dim AcadFilter As Decimal = F200_SBS_Academies
        If AcadFilter = 1 Then
            result = P110_IDACIASecSubtotal * P111a_NSENIDACIASec_Percent
        Else
            exclude(rid)
        End if

        Print(P110_IDACIASecSubtotal, "P110_IDACIASecSubtotal", rid)
        Print(P111a_NSENIDACIASec_Percent, "P111a_NSENIDACIASec_Percent", rid)
        Return result
    End Function

    <Calculation(Id:="72b731f7bc2a4c1eaeeb2227fe197ed1")>
    <CalculationSpecification(Id:="P111a_NSENIDACIASec_Percent", Name:="P111a_NSENIDACIASec_Percent")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="86e2181721da4a669a09cccbafc502f1", Name:="IDACI")>
    Public Function P111a_NSENIDACIASec_Percent As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim Notional_SEN As Decimal = LAtoProv(Datasets.APTProformadataset.IDACIBASecondaryNotionalSEN)
        Dim AcadFilter As Decimal = F200_SBS_Academies
        If AcadFilter = 1 then
            result = Notional_SEN * 100
        Else
            exclude(rid)
        End if

        Print(Notional_SEN, "Notional_SEN", rid)
        Return result
    End Function

    <Calculation(Id:="d41e012c4ecd46d0bf268bb1b77a9dec")>
    <CalculationSpecification(Id:="P112_InYearIDACIASecSubtotal", Name:="P112_InYearIDACIASecSubtotal")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="86e2181721da4a669a09cccbafc502f1", Name:="IDACI")>
    Public Function P112_InYearIDACIASecSubtotal As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim P110_IDACIASecSubtotal As Decimal = P110_IDACIASecSubtotal
        Dim P001_1718DaysOpen As Decimal = P001_1718DaysOpen
        Dim Year_Days1718 As Decimal = P025_YearDays_1718
        Dim AcadFilter As Decimal = F200_SBS_Academies
        If AcadFilter = 1 Then
            Result = P110_IDACIASecSubtotal * (P001_1718DaysOpen / Year_Days1718)
        Else
            exclude(rid)
        End if

        Print(P110_IDACIASecSubtotal, "P110_IDACIASecSubtotal", rid)
        Print(P001_1718DaysOpen, "P001_1718DaysOpen", rid)
        Print(Year_Days1718, "Year_Days1718", rid)
        Return result
    End Function

    <Calculation(Id:="9574dd23000c4ac3b81888efc14d584b")>
    <CalculationSpecification(Id:="P114_LACFactor", Name:="P114_LACFactor")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="3e4d786c0a984c6c9eb92a959ef1e5f5", Name:="LAC")>
    Public Function P114_LACFactor As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim F100_AllAcademies As Decimal = F100_AllAcademies
        Dim FundingBasis As String = F900_FundingBasis
        Dim F200_SBS_Academies As Decimal = F200_SBS_Academies
        Dim LACCensus As Decimal = Datasets.CensusPupilCharacteristics.LACXProportion
        Dim LACAdj As Decimal = Datasets.APTInputsandAdjustments.LACXProportion
        Dim LACAdjString as string = Datasets.APTInputsandAdjustments.LACXProportion
        Dim LACCensusString as string = Datasets.CensusPupilCharacteristics.LACXProportion
        Dim LA_AV As Decimal = latoprov(Datasets.LocalAuthorityAverages.LACXProportion)
        If F200_SBS_Academies = 1 then
            If string.IsNullOrEmpty(LACAdjString) And string.IsNullOrEmpty(LACCensusString) THEN
                Result = 0
            else
                If string.IsNullOrEmpty(LACAdjString) THEN
                    Result = LACCensus
                ELSE
                    Result = LACAdj
                End if
            End If
        else
            exclude(rid)
        End if

        Print(F100_AllAcademies, "F100_AllAcademies", rid)
        Print(FundingBasis, "FundingBasis", rid)
        Print(LA_AV, "LA average", rid)
        Print(LACCensus, "LACCensus", rid)
        Print(LACAdj, "LACAdj", rid)
        Print(LACAdjString, "LACAdjString", rid)
        Print(LACCensusString, "LACCensusString", rid)
        Return result
    End Function

    <Calculation(Id:="ef7a5397b43a4a9e966734fad4ff0f45")>
    <CalculationSpecification(Id:="P116_LACRate", Name:="P116_LACRate")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="3e4d786c0a984c6c9eb92a959ef1e5f5", Name:="LAC")>
    Public Function P116_LACRate As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim F100_AllAcademies As Decimal = F100_AllAcademies
        Dim FundingBasis As String = F900_FundingBasis
        Dim F200_SBS_Academies As Decimal = F200_SBS_Academies
        Dim Looked_After_Children_Amount_Per_Pupil As Decimal = LatoProv(Datasets.APTProformadataset.LookedAfterChildrenAmountPerPupil)
        If F200_SBS_Academies = 1 then
            Result = Looked_After_Children_Amount_Per_Pupil
        else
            exclude(rid)
        End if

        Print(F100_AllAcademies, "F100_AllAcademies", rid)
        Print(FundingBasis, "FundingBasis", rid)
        Print(F200_SBS_Academies, "F200_SBS_Academies", rid)
        Print(Looked_After_Children_Amount_Per_Pupil, "Looked_After_Children_Amount_Per_Pupil", rid)
        Return result
    End Function

    <Calculation(Id:="e445010f517f46758944a7d83c350d24")>
    <CalculationSpecification(Id:="P117_LACSubtotal", Name:="P117_LACSubtotal")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="3e4d786c0a984c6c9eb92a959ef1e5f5", Name:="LAC")>
    Public Function P117_LACSubtotal As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim F100_AllAcademies As Decimal = F100_AllAcademies
        Dim FundingBasis As decimal = F900_FundingBasis
        Dim F200_SBS_Academies As Decimal = F200_SBS_Academies
        Dim P114_LACFactor As Decimal = P114_LACFactor
        Dim P116_LACRate As Decimal = P116_LACRate
        Dim P26_Total_NOR_SBS As Decimal = NOR_P26_Total_NOR_SBS
        Dim P117_LACSubtotal As Decimal = P26_Total_NOR_SBS * P116_LACRate * P114_LACFactor
        Dim LAC As Decimal = Datasets.APTNewISBdataset.IDACI(SA)
        If F200_SBS_Academies <> 1 Then
            exclude(rid)
        End if

        If(F100_AllAcademies = 17181) Or (FundingBasis = 2 And F100_AllAcademies = 17182) Or (FundingBasis = 2 And F100_AllAcademies = 17183) then
            result = P117_LACSubtotal
        Else
            If(FundingBasis = 1 And F100_AllAcademies = 17182) Or (FundingBasis = 1 And F100_AllAcademies = 17183) then 'new opener
                Result = LAC
            End If
        End If

        Print(F100_AllAcademies, "F100_AllAcademies", rid)
        Print(FundingBasis, "FundingBasis", rid)
        Print(F200_SBS_Academies, "F200_SBS_Academies", rid)
        Print(P114_LACFactor, "P114_LACFactor", rid)
        Print(P116_LACRate, "P116_LACRate", rid)
        Print(P26_Total_NOR_SBS, "P26_Total_NOR_SBS", rid)
        Print(LAC, "LAC", rid)
        Return result
    End Function

    <Calculation(Id:="a1915bf39d5242959f272f6ff5f762f5")>
    <CalculationSpecification(Id:="P118_NSENLAC", Name:="P118_NSENLAC")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="3e4d786c0a984c6c9eb92a959ef1e5f5", Name:="LAC")>
    Public Function P118_NSENLAC As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim F100_AllAcademies As Decimal = F100_AllAcademies
        Dim FundingBasis As String = F900_FundingBasis
        Dim F200_SBS_Academies As Decimal = F200_SBS_Academies
        Dim P117_LACSubtotal As Decimal = P117_LACSubtotal
        Dim P118a_NSENLAC_Percent As Decimal = P118a_NSENLAC_Percent / 100
        If F200_SBS_Academies = 1 Then
            Result = P117_LACSubtotal * P118a_NSENLAC_Percent
        Else
            exclude(rid)
        End if

        Print(F100_AllAcademies, "F100_AllAcademies", rid)
        Print(FundingBasis, "FundingBasis", rid)
        Print(F200_SBS_Academies, "F200_SBS_Academies", rid)
        Print(P117_LACSubtotal, "P117_LACSubtotal", rid)
        Print(P118a_NSENLAC_Percent, "P118a_NSENLAC_Percent", rid)
        Return result
    End Function

    <Calculation(Id:="78d4c63196614e51b0815a389573fc2c")>
    <CalculationSpecification(Id:="P118a_NSENLAC_Percent", Name:="P118a_NSENLAC_Percent")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="3e4d786c0a984c6c9eb92a959ef1e5f5", Name:="LAC")>
    Public Function P118a_NSENLAC_Percent As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim F100_AllAcademies As Decimal = F100_AllAcademies
        Dim FundingBasis As String = F900_FundingBasis
        Dim F200_SBS_Academies As Decimal = F200_SBS_Academies
        Dim Looked_After_Children_Notional_SEN As Decimal = LatoProv(Datasets.APTProformadataset.LookedAfterChildrenNotionalSEN)
        If F200_SBS_Academies = 1 then
            Result = Looked_After_Children_Notional_SEN * 100
        else
            exclude(rid)
        End if

        Print(F100_AllAcademies, "F100_AllAcademies", rid)
        Print(FundingBasis, "FundingBasis", rid)
        Print(F200_SBS_Academies, "F200_SBS_Academies", rid)
        Print(Looked_After_Children_Notional_SEN, "Looked_After_Children_Notional_SEN", rid)
        Return result
    End Function

    <Calculation(Id:="4f682331d1b0409cb1fccc37a33a728a")>
    <CalculationSpecification(Id:="P119_InYearLACSubtotal", Name:="P119_InYearLACSubtotal")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="3e4d786c0a984c6c9eb92a959ef1e5f5", Name:="LAC")>
    Public Function P119_InYearLACSubtotal As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim F100_AllAcademies As Decimal = F100_AllAcademies
        Dim FundingBasis As String = F900_FundingBasis
        Dim F200_SBS_Academies As Decimal = F200_SBS_Academies
        Dim P117_LACSubtotal As Decimal = P117_LACSubtotal
        Dim Days_Open As Decimal = P001_1718DaysOpen
        Dim Year_Days As Decimal = 365
        If F200_SBS_Academies = 1 Then
            Result = P117_LACSubtotal * Days_Open / Year_Days
        Else
            exclude(rid)
        End if

        Print(F100_AllAcademies, "F100_AllAcademies", rid)
        Print(FundingBasis, "FundingBasis", rid)
        Print(F200_SBS_Academies, "F200_SBS_Academies", rid)
        Print(P117_LACSubtotal, "P117_LACSubtotal", rid)
        Print(Days_Open, "Days_Open", rid)
        Print(Year_Days, "Year_Days", rid)
        Return result
    End Function

    <Calculation(Id:="6307dbb64d2940daaab34784b46fcd4a")>
    <CalculationSpecification(Id:="P174_MobPriFactor", Name:="P174_MobPriFactor")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="41e8854d7b7e4ecdb46bbc643a62d19e", Name:="Mobility")>
    Public Function P174_MobPriFactor As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim [Census_Pupil_Characteristics_Mobility_Primary_Proportion] As Decimal = Datasets.CensusPupilCharacteristics.MobilityPrimaryProportion
        Dim [APT_Inputs_and_Adjustments_Mobility_Primary_Proportion] As Decimal = Datasets.APTInputsandAdjustments.MobilityPrimaryProportion
        Dim AcadFilter As Decimal = F200_SBS_Academies
        Dim ModPriProAdjString as string = Datasets.APTInputsandAdjustments.MobilityPrimaryProportion
        If AcadFilter = 1 Then
            If string.IsNullOrEmpty(ModPriProAdjString) Then
                If [Census_Pupil_Characteristics_Mobility_Primary_Proportion] > 0.1 Then
                    result = [Census_Pupil_Characteristics_Mobility_Primary_Proportion] - 0.1
                Else
                    result = 0
                End if
            Else
                If [APT_Inputs_and_Adjustments_Mobility_Primary_Proportion] > 0.1 Then
                    result = [APT_Inputs_and_Adjustments_Mobility_Primary_Proportion] - 0.1
                Else
                    result = 0
                End if
            End if
        Else
            exclude(rid)
        End if

        Print(Census_Pupil_Characteristics_Mobility_Primary_Proportion, "Census_Pupil_Characteristics_Mobility_Primary_Proportion", rid)
        Print(APT_Inputs_and_Adjustments_Mobility_Primary_Proportion, "APT_Inputs_and_Adjustments_Mobility_Primary_Proportion", rid)
        Return result
    End Function

    <Calculation(Id:="e23977cabdf3441b98ade465117975d6")>
    <CalculationSpecification(Id:="P176_MobPriRate", Name:="P176_MobPriRate")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="41e8854d7b7e4ecdb46bbc643a62d19e", Name:="Mobility")>
    Public Function P176_MobPriRate As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim APT_PP As Decimal = LAtoProv(Datasets.APTProformadataset.MobilityPrimaryAmountPerPupil)
        Dim AcadFilter As Decimal = F200_SBS_Academies
        If AcadFilter = 1 Then
            result = APT_PP
        Else
            exclude(rid)
        End if

        Print(APT_PP, "APT_PP", rid)
        Return result
    End Function

    <Calculation(Id:="bc653b86c1b4486fb1807c508e9b5953")>
    <CalculationSpecification(Id:="P177_MobPriSubtotal", Name:="P177_MobPriSubtotal")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="41e8854d7b7e4ecdb46bbc643a62d19e", Name:="Mobility")>
    Public Function P177_MobPriSubtotal As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim SBS_P22 As Decimal = NOR_P22_Total_NOR_PRI_SBS
        Dim P174_MobPriFactor As Decimal = P174_MobPriFactor
        Dim P176_MobPriRate As Decimal = P176_MobPriRate
        Dim APT_ISB_Mobility_Primary As Decimal = Datasets.APTNewISBdataset.MobilityP
        Dim F100_AllAcademies As Decimal = F100_AllAcademies
        Dim FundingBasis As Decimal = F900_FundingBasis
        Dim F200 As Decimal = F200_SBS_Academies
        If F200 = 1 then
            If F100_AllAcademies = 17181 Or (F100_AllAcademies = 17182 And FundingBasis = 2) Or (F100_AllAcademies = 17183 And FundingBasis = 2) Then
                Result = SBS_P22 * P176_MobPriRate * P174_MobPriFactor
            ElseIf F100_AllAcademies <> 17181 And FundingBasis = 1 Then
                result = APT_ISB_Mobility_Primary
            Else
                Exclude(rid)
            End If
        Else
            exclude(rid)
        End if

        Print(SBS_P22, "SBS_P22", rid)
        Print(P174_MobPriFactor, "P174_MobPriFactor", rid)
        Print(P176_MobPriRate, "P176_MobPriRate", rid)
        Print(APT_ISB_Mobility_Primary, "APT_ISB_Mobility_Primary", rid)
        Print(F100_AllAcademies, "F100_AllAcademies", rid)
        Print(FundingBasis, "Funding_Basis", rid)
        Return result
    End Function

    <Calculation(Id:="03a959e60df54699a77ef50214135286")>
    <CalculationSpecification(Id:="P178_NSENMobPri", Name:="P178_NSENMobPri")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="41e8854d7b7e4ecdb46bbc643a62d19e", Name:="Mobility")>
    Public Function P178_NSENMobPri As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim P177_MobPriSubtotal As Decimal = P177_MobPriSubtotal
        Dim P178a_NSENMobPri_Percent As Decimal = P178a_NSENMobPri_Percent / 100
        Dim AcadFilter As Decimal = F200_SBS_Academies
        If AcadFilter = 1 Then
            result = P177_MobPriSubtotal * P178a_NSENMobPri_Percent
        Else
            exclude(rid)
        End if

        Print(P177_MobPriSubtotal, "P177_MobPriSubtotal", rid)
        Print(P178a_NSENMobPri_Percent, "P178a_NSENMobPri_Percent", rid)
        Return result
    End Function

    <Calculation(Id:="38a11a4fee7d4865b714c6b6edcacfb1")>
    <CalculationSpecification(Id:="P178a_NSENMobPri_Percent", Name:="P178a_NSENMobPri_Percent")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="41e8854d7b7e4ecdb46bbc643a62d19e", Name:="Mobility")>
    Public Function P178a_NSENMobPri_Percent As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim Notional_SEN As Decimal = LAtoProv(Datasets.APTProformadataset.MobilityPrimaryNotionalSEN)
        Dim AcadFilter As Decimal = F200_SBS_Academies
        If AcadFilter = 1 then
            result = Notional_SEN * 100
        Else
            exclude(rid)
        End if

        Print(Notional_SEN, "Notional_SEN", rid)
        Return result
    End Function

    <Calculation(Id:="02af0e75936241bf87bdfa04715762b1")>
    <CalculationSpecification(Id:="P179_InYearMobPriSubtotal", Name:="P179_InYearMobPriSubtotal")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="41e8854d7b7e4ecdb46bbc643a62d19e", Name:="Mobility")>
    Public Function P179_InYearMobPriSubtotal As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim P177_MobPriSubtotal As Decimal = P177_MobPriSubtotal
        Dim P001_1718DaysOpen As Decimal = P001_1718DaysOpen
        Dim Year_Days1718 As Decimal = P025_YearDays_1718
        Dim AcadFilter As Decimal = F200_SBS_Academies
        If AcadFilter = 1 Then
            result = P177_MobPriSubtotal * (P001_1718DaysOpen / Year_Days1718)
        Else
            exclude(rid)
        End if

        Print(P177_MobPriSubtotal, "P177_MobPriSubtotal", rid)
        Print(P001_1718DaysOpen, "P001_1718DaysOpen", rid)
        Print(Year_Days1718, "Year_Days1718", rid)
        Return result
    End Function

    <Calculation(Id:="48a163962ec04780bd82bbfea4267a23")>
    <CalculationSpecification(Id:="P180_MobSecFactor", Name:="P180_MobSecFactor")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="41e8854d7b7e4ecdb46bbc643a62d19e", Name:="Mobility")>
    Public Function P180_MobSecFactor As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim [Census_Pupil_Characteristics_Mobility_Secondary_Proportion] As Decimal = Datasets.CensusPupilCharacteristics.MobilitySecondaryProportion
        Dim [APT_Inputs_and_Adjustments_Mobility_Secondary_Proportion] As Decimal = Datasets.APTInputsandAdjustments.MobilitySecondaryProportion
        Dim AcadFilter As Decimal = F200_SBS_Academies
        Dim MobSecProAdj as string = Datasets.APTInputsandAdjustments.MobilitySecondaryProportion
        If AcadFilter = 1 Then
            If string.IsNullOrEmpty(MobSecProAdj) Then
                If [Census_Pupil_Characteristics_Mobility_Secondary_Proportion] > 0.1 Then
                    result = [Census_Pupil_Characteristics_Mobility_Secondary_Proportion] - 0.1
                Else
                    result = 0
                End if
            Else
                If [APT_Inputs_and_Adjustments_Mobility_Secondary_Proportion] > 0.1 Then
                    result = [APT_Inputs_and_Adjustments_Mobility_Secondary_Proportion] - 0.1
                Else
                    result = 0
                End if
            End if
        Else
            exclude(rid)
        End if

        Print(Census_Pupil_Characteristics_Mobility_Secondary_Proportion, "Census_Pupil_Characteristics_Mobility_Secondary_Proportion", rid)
        Print(APT_Inputs_and_Adjustments_Mobility_Secondary_Proportion, "APT_Inputs_and_Adjustments_Mobility_Secondary_Proportion", rid)
        Return result
    End Function

    <Calculation(Id:="54a9c124d47e4bdda2427bcddeea3939")>
    <CalculationSpecification(Id:="P182_MobSecRate", Name:="P182_MobSecRate")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="41e8854d7b7e4ecdb46bbc643a62d19e", Name:="Mobility")>
    Public Function P182_MobSecRate As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim APT_PP As Decimal = LAtoProv(Datasets.APTProformadataset.MobilitySecondaryAmountPerPupil)
        Dim AcadFilter As Decimal = F200_SBS_Academies
        If AcadFilter = 1 Then
            result = APT_PP
        Else
            exclude(rid)
        End if

        Print(APT_PP, "APT_PP", rid)
        Return result
    End Function

    <Calculation(Id:="b8e94cf936504e69a58a19f726c0fb44")>
    <CalculationSpecification(Id:="P183_MobSecSubtotal", Name:="P183_MobSecSubtotal")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="41e8854d7b7e4ecdb46bbc643a62d19e", Name:="Mobility")>
    Public Function P183_MobSecSubtotal As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim SBS_P25 As Decimal = NOR_P25_Total_NOR_SEC_SBS
        Dim P180_MobSecFactor As Decimal = P180_MobSecFactor
        Dim P182_MobSecRate As Decimal = P182_MobSecRate
        Dim APT_ISB_Mobility_Secondary As Decimal = Datasets.APTNewISBdataset.MobilityS
        Dim F100_AllAcademies As Decimal = F100_AllAcademies
        Dim FundingBasis As Decimal = F900_FundingBasis
        Dim F200 As Decimal = F200_SBS_Academies
        If F200 = 1 then
            If F100_AllAcademies = 17181 Or (F100_AllAcademies = 17182 And FundingBasis = 2) Or (F100_AllAcademies = 17183 And FundingBasis = 2) Then
                Result = SBS_P25 * P182_MobSecRate * P180_MobSecFactor
            ElseIf F100_AllAcademies <> 17181 And FundingBasis = 1 Then
                result = APT_ISB_Mobility_Secondary
            Else
                Exclude(rid)
            End If
        else
            exclude(rid)
        End if

        Print(SBS_P25, "SBS_P25", rid)
        Print(P180_MobSecFactor, "P180_MobSecFactor", rid)
        Print(P182_MobSecRate, "P182_MobSecRate", rid)
        Print(APT_ISB_Mobility_Secondary, "APT_ISB_Mobility_Secondary", rid)
        Print(F100_AllAcademies, "F100_AllAcademies", rid)
        Print(FundingBasis, "Funding_Basis", rid)
        Return result
    End Function

    <Calculation(Id:="4c7142eff0ee4399a51560bd3ca7ddba")>
    <CalculationSpecification(Id:="P184_NSENMobSec", Name:="P184_NSENMobSec")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="41e8854d7b7e4ecdb46bbc643a62d19e", Name:="Mobility")>
    Public Function P184_NSENMobSec As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim P183_MobSecSubtotal As Decimal = P183_MobSecSubtotal
        Dim P184a_NSENMobSec_Percent As Decimal = P184a_NSENMobSec_Percent / 100
        Dim AcadFilter As Decimal = F200_SBS_Academies
        If AcadFilter = 1 Then
            result = P184a_NSENMobSec_Percent * P183_MobSecSubtotal
        Else
            exclude(rid)
        End if

        Print(P183_MobSecSubtotal, "P183_MobSecSubtotal", rid)
        Print(P184a_NSENMobSec_Percent, "P184a_NSENMobSec_Percent", rid)
        Return result
    End Function

    <Calculation(Id:="9f3b7732e4ca454386e47812beef8038")>
    <CalculationSpecification(Id:="P184a_NSENMobSec_Percent", Name:="P184a_NSENMobSec_Percent")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="41e8854d7b7e4ecdb46bbc643a62d19e", Name:="Mobility")>
    Public Function P184a_NSENMobSec_Percent As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim Notional_SEN As Decimal = LAtoProv(Datasets.APTProformadataset.MobilitySecondaryNotionalSEN)
        Dim AcadFilter As Decimal = F200_SBS_Academies
        If AcadFilter = 1 then
            result = Notional_SEN * 100
        Else
            exclude(rid)
        End if

        Print(Notional_SEN, "Notional_SEN", rid)
        Return result
    End Function

    <Calculation(Id:="2f2cbe3d03124cb3814bb96494746cb0")>
    <CalculationSpecification(Id:="P185_InYearMobSecSubtotal", Name:="P185_InYearMobSecSubtotal")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="41e8854d7b7e4ecdb46bbc643a62d19e", Name:="Mobility")>
    Public Function P185_InYearMobSecSubtotal As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim P183_MobSecSubtotal As Decimal = P183_MobSecSubtotal
        Dim P001_1718DaysOpen As Decimal = P001_1718DaysOpen
        Dim Year_Days1718 As Decimal = P025_YearDays_1718
        Dim AcadFilter As Decimal = F200_SBS_Academies
        If AcadFilter = 1 Then
            result = P183_MobSecSubtotal * (P001_1718DaysOpen / Year_Days1718)
        Else
            exclude(rid)
        End if

        Print(P183_MobSecSubtotal, "P183_MobSecSubtotal", rid)
        Print(P001_1718DaysOpen, "P001_1718DaysOpen", rid)
        Print(Year_Days1718, "Year_Days1718", rid)
        Return result
    End Function

    <Calculation(Id:="78e47df516624023943553ea8e2fa937")>
    <CalculationSpecification(Id:="P239_PriLumpSumFactor", Name:="P239_PriLumpSumFactor")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="41080d8da69046e3a26ff4fb6894eaa8", Name:="Other Factors")>
    Public Function P239_PriLumpSumFactor As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim AcadFilter As Decimal = F200_SBS_Academies
        Dim P212_PYG As Decimal = P212_PYG
        Dim P213_SYG As Decimal = P213_SYG
        Dim Phase As Decimal = P185a_Phase
        If AcadFilter = 1 Then
            If P212_PYG = 0 And P213_SYG = 0 Then
                result = 0
            Else
                If Phase = 1 Then
                    result = 1
                Else If Phase = 2 Or Phase = 4 Then
                    result = P212_PYG / (P212_PYG + P213_SYG)
                Else
                    result = 0
                End If
            End If
        Else
            exclude(rid)
        End If

        Print(Phase, "Phase", rid)
        Print(P212_PYG, "P212_PYG", rid)
        Print(P213_SYG, "P213_SYG", rid)
        Return result
    End Function

    <Calculation(Id:="8f8a51340d054e06adc1e80bbd5cdd4a")>
    <CalculationSpecification(Id:="P240_PriLumpSumRate", Name:="P240_PriLumpSumRate")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="41080d8da69046e3a26ff4fb6894eaa8", Name:="Other Factors")>
    Public Function P240_PriLumpSumRate As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim PrimaryLumpSum As Decimal = LAtoProv(Datasets.APTProformadataset.PrimaryLumpSum)
        Dim AcadFilter As Decimal = F200_SBS_Academies
        If AcadFilter = 1 Then
            result = PrimaryLumpSum
        Else
            exclude(rid)
        End if

        Print(PrimaryLumpSum, "PrimaryLumpSum", rid)
        Return result
    End Function

    <Calculation(Id:="6ef99204c60e4a84ac2981ba623ab2e2")>
    <CalculationSpecification(Id:="P241_Primary_Lump_Sum", Name:="P241_Primary_Lump_Sum")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="41080d8da69046e3a26ff4fb6894eaa8", Name:="Other Factors")>
    Public Function P241_Primary_Lump_Sum As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="fbe97c50ea364e24af418eeccafafcde")>
    <CalculationSpecification(Id:="P242_InYearPriLumpSumSubtotal", Name:="P242_InYearPriLumpSumSubtotal")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="41080d8da69046e3a26ff4fb6894eaa8", Name:="Other Factors")>
    Public Function P242_InYearPriLumpSumSubtotal As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim P241_PriLumpSumSubtotal As Decimal = P241_PriLumpSumSubtotal
        Dim P001_1718DaysOpen As Decimal = P001_1718DaysOpen
        Dim Year_Days1718 As Decimal = P025_YearDays_1718
        Dim AcadFilter As Decimal = F200_SBS_Academies
        If AcadFilter = 1 Then
            result = P241_PriLumpSumSubtotal * (P001_1718DaysOpen / Year_Days1718)
        Else
            exclude(rid)
        End if

        Print(P241_PriLumpSumSubtotal, "P241_PriLumpSumSubtotal", rid)
        Print(P001_1718DaysOpen, "P001_1718DaysOpen", rid)
        Print(Year_Days1718, "Year_Days1718", rid)
        Return result
    End Function

    <Calculation(Id:="bac03e506bff4c318040707999277d81")>
    <CalculationSpecification(Id:="P243_SecLumpSumFactor", Name:="P243_SecLumpSumFactor")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="41080d8da69046e3a26ff4fb6894eaa8", Name:="Other Factors")>
    Public Function P243_SecLumpSumFactor As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim AcadFilter As Decimal = F200_SBS_Academies
        Dim P212_PYG As Decimal = P212_PYG
        Dim P213_SYG As Decimal = P213_SYG
        Dim Phase As Decimal = P185a_Phase
        If AcadFilter = 1 Then
            If P212_PYG = 0 And P213_SYG = 0 Then
                result = 0
            Else
                If Phase = 3 Or Phase = 5 Then
                    result = 1
                Else If Phase = 2 Or Phase = 4 Then
                    result = P213_SYG / (P212_PYG + P213_SYG)
                Else
                    result = 0
                End If
            End If
        Else
            exclude(rid)
        End If

        Print(Phase, "Phase", rid)
        Print(P212_PYG, "P212_PYG", rid)
        Print(P213_SYG, "P213_SYG", rid)
        Return result
    End Function

    <Calculation(Id:="b6de99ad44b949cbb67d26fc5b46050b")>
    <CalculationSpecification(Id:="P244_SecLumpSumRate", Name:="P244_SecLumpSumRate")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="41080d8da69046e3a26ff4fb6894eaa8", Name:="Other Factors")>
    Public Function P244_SecLumpSumRate As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim SecondaryLumpSum As Decimal = LAtoProv(Datasets.APTProformadataset.SecondaryLumpSum)
        Dim AcadFilter As Decimal = F200_SBS_Academies
        If AcadFilter = 1 Then
            result = SecondaryLumpSum
        Else
            exclude(rid)
        End if

        Print(SecondaryLumpSum, "SecondaryLumpSum", rid)
        Return result
    End Function

    <Calculation(Id:="d3891eb75b074373a3c83f4d011ede44")>
    <CalculationSpecification(Id:="P245_Secondary_Lump_Sum", Name:="P245_Secondary_Lump_Sum")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="41080d8da69046e3a26ff4fb6894eaa8", Name:="Other Factors")>
    Public Function P245_Secondary_Lump_Sum As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="a9f66d6873e140a3996505ebc2c87572")>
    <CalculationSpecification(Id:="P246_In YearSecLumpSumSubtotal", Name:="P246_In YearSecLumpSumSubtotal")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="41080d8da69046e3a26ff4fb6894eaa8", Name:="Other Factors")>
    Public Function P246_InYearSecLumpSumSubtotal As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="72b866cf1666421198bf837e04dded8d")>
    <CalculationSpecification(Id:="P247_NSENLumpSumPri", Name:="P247_NSENLumpSumPri")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="41080d8da69046e3a26ff4fb6894eaa8", Name:="Other Factors")>
    Public Function P247_NSENLumpSumPri As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim P241_PriLumpSumSubtotal As Decimal = P241_PriLumpSumSubtotal
        Dim P247a_NSENLumpSumPri_Percent As Decimal = P247a_NSENLumpSumPri_Percent / 100
        Dim AcadFilter As Decimal = F200_SBS_Academies
        If AcadFilter = 1 Then
            result = P241_PriLumpSumSubtotal * P247a_NSENLumpSumPri_Percent
        Else
            exclude(rid)
        End if

        Print(P241_PriLumpSumSubtotal, "P241_PriLumpSumSubtotal", rid)
        Print(P247a_NSENLumpSumPri_Percent, "P247a_NSENLumpSumPri_Percent", rid)
        Return result
    End Function

    <Calculation(Id:="f5c83bb476194c3dab18c1330ad390d3")>
    <CalculationSpecification(Id:="P247a_NSENLumpSumPri_Percent", Name:="P247a_NSENLumpSumPri_Percent")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="41080d8da69046e3a26ff4fb6894eaa8", Name:="Other Factors")>
    Public Function P247a_NSENLumpSumPri_Percent As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim Notional_SEN As Decimal = LAtoProv(Datasets.APTProformadataset.PrimaryLumpSumNotionalSEN)
        Dim AcadFilter As Decimal = F200_SBS_Academies
        If AcadFilter = 1 then
            result = Notional_SEN * 100
        Else
            exclude(rid)
        End if

        Print(Notional_SEN, "Notional_SEN", rid)
        Return result
    End Function

    <Calculation(Id:="57ab7ec0ac384e06a3b07b42ba95c225")>
    <CalculationSpecification(Id:="P248_NSENLumpSumSec", Name:="P248_NSENLumpSumSec")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="41080d8da69046e3a26ff4fb6894eaa8", Name:="Other Factors")>
    Public Function P248_NSENLumpSumSec As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim P245_SecLumpSumSubtotal As Decimal = P245_SecLumpSumSubtotal
        Dim P248a_NSENLumpSumSec_Percent As Decimal = P248a_NSENLumpSumSec_Percent / 100
        Dim AcadFilter As Decimal = F200_SBS_Academies
        If AcadFilter = 1 Then
            result = P245_SecLumpSumSubtotal * P248a_NSENLumpSumSec_Percent
        Else
            exclude(rid)
        End if

        Print(P245_SecLumpSumSubtotal, "P245_SecLumpSumSubtotal", rid)
        Print(P248a_NSENLumpSumSec_Percent, "P248a_NSENLumpSumSec_Percent", rid)
        Return result
    End Function

    <Calculation(Id:="5d83169e36a446d888dd24ccc6055262")>
    <CalculationSpecification(Id:="P248a_NSENLumpSumSec_Percent", Name:="P248a_NSENLumpSumSec_Percent")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="41080d8da69046e3a26ff4fb6894eaa8", Name:="Other Factors")>
    Public Function P248a_NSENLumpSumSec_Percent As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim Notional_SEN As Decimal = LAtoProv(Datasets.APTProformadataset.SecondaryLumpSumNotionalSEN)
        Dim AcadFilter As Decimal = F200_SBS_Academies
        If AcadFilter = 1 then
            result = Notional_SEN * 100
        Else
            exclude(rid)
        End if

        Print(Notional_SEN, "Notional_SEN", rid)
        Return result
    End Function

    <Calculation(Id:="e6930c5a9c044f8288c25bd94ccf7055")>
    <CalculationSpecification(Id:="P252_PFISubtotal", Name:="P252_PFISubtotal")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="41080d8da69046e3a26ff4fb6894eaa8", Name:="Other Factors")>
    Public Function P252_PFISubtotal As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim AcadFilter As Decimal = F200_SBS_Academies
        Dim PFIString As String = Datasets.APTNewISBdataset.PFI
        Dim ISB_PFI As Decimal = Datasets.APTNewISBdataset.PFI
        If AcadFilter = 1 Then
            If string.IsNullOrEmpty(PFIString) Then
                result = 0
            Else
                result = ISB_PFI
            End If
        Else
            exclude(rid)
        End If

        Print(ISB_PFI, "ISB_PFI", rid)
        Return result
    End Function

    <Calculation(Id:="c7a417d0acb842c8ab0d45c8cdfdbb02")>
    <CalculationSpecification(Id:="P253_NSENPFI", Name:="P253_NSENPFI")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="41080d8da69046e3a26ff4fb6894eaa8", Name:="Other Factors")>
    Public Function P253_NSENPFI As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim P252_PFISubtotal As Decimal = P252_PFISubtotal
        Dim P253a_NSENPFI_Percent As Decimal = P253a_NSENPFI_Percent / 100
        Dim AcadFilter As Decimal = F200_SBS_Academies
        If AcadFilter = 1 Then
            result = P252_PFISubtotal * P253a_NSENPFI_Percent
        Else
            exclude(rid)
        End if

        Print(P252_PFISubtotal, "P252_PFISubtotal", rid)
        Print(P253a_NSENPFI_Percent, "P253a_NSENPFI_Percent", rid)
        Return result
    End Function

    <Calculation(Id:="2ed32818f8dd4aabaccd2ad43fb5c860")>
    <CalculationSpecification(Id:="P253a_NSENPFI_Percent", Name:="P253a_NSENPFI_Percent")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="41080d8da69046e3a26ff4fb6894eaa8", Name:="Other Factors")>
    Public Function P253a_NSENPFI_Percent As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim Notional_SEN As Decimal = LAtoProv(Datasets.APTProformadataset.PFINotionalSEN)
        Dim AcadFilter As Decimal = F200_SBS_Academies
        If AcadFilter = 1 then
            result = Notional_SEN * 100
        Else
            exclude(rid)
        End if

        Print(Notional_SEN, "Notional_SEN", rid)
        Return result
    End Function

    <Calculation(Id:="833750cb454c44369cce1d5461ad893d")>
    <CalculationSpecification(Id:="P254_InYearPFISubtotal", Name:="P254_InYearPFISubtotal")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="41080d8da69046e3a26ff4fb6894eaa8", Name:="Other Factors")>
    Public Function P254_InYearPFISubtotal As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim P252_PFISubtotal As Decimal = P252_PFISubtotal
        Dim P001_1718DaysOpen As Decimal = P001_1718DaysOpen
        Dim Year_Days1718 As Decimal = P025_YearDays_1718
        Dim AcadFilter As Decimal = F200_SBS_Academies
        If AcadFilter = 1 Then
            result = P252_PFISubtotal * (P001_1718DaysOpen / Year_Days1718)
        Else
            exclude(rid)
        End if

        Print(P252_PFISubtotal, "P252_PFISubtotal", rid)
        Print(P001_1718DaysOpen, "P001_1718DaysOpen", rid)
        Print(Year_Days1718, "Year_Days1718", rid)
        Return result
    End Function

    <Calculation(Id:="e1629bfae37d47a3bed0a42be199d31b")>
    <CalculationSpecification(Id:="P255_FringeSubtotal", Name:="P255_FringeSubtotal")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="41080d8da69046e3a26ff4fb6894eaa8", Name:="Other Factors")>
    Public Function P255_FringeSubtotal As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim APT_ISB_LondonFringe As Decimal = Datasets.APTNewISBdataset.LondonFringe
        Dim F100_AllAcademies As Decimal = F100_AllAcademies
        Dim FundingBasis As Decimal = F900_FundingBasis
        Dim LondonFringeIandA As Decimal = Datasets.APTInputsandAdjustments.LondonFringe
        Dim LondonFringeCensus As Decimal = Datasets.CensusPupilCharacteristics.LondonFringe
        Dim FringeIandAString As String = Datasets.APTInputsandAdjustments.LondonFringe
        Dim FringeCensusString As string = Datasets.CensusPupilCharacteristics.LondonFringe
        Dim LondonFringeCensusTest As Decimal = 0
        Dim LondonFringeIandATest As Decimal = 0
        Dim P005 As Decimal = P005_PriBESubtotal
        Dim P010 As Decimal = P010_KS3_BESubtotal
        Dim P015 As Decimal = P015_KS4_BESubtotal
        Dim P022 As Decimal = P022_PriFSMSubtotal
        Dim P027 As Decimal = P027_PriFSM6Subtotal
        Dim P033 As Decimal = P033_SecFSMSubtotal
        Dim P038 As Decimal = P038_SecFSM6Subtotal
        Dim P044 As Decimal = P044_IDACIFPriSubtotal
        Dim P050 As Decimal = P050_IDACIEPriSubtotal
        Dim P056 As Decimal = P056_IDACIDPriSubtotal
        Dim P062 As Decimal = P062_IDACICPriSubtotal
        Dim P068 As Decimal = P068_IDACIBPriSubtotal
        Dim P074 As Decimal = P074_IDACIAPriSubtotal
        Dim P080 As Decimal = P080_IDACIFSecSubtotal
        Dim P086 As Decimal = P086_IDACIESecSubtotal
        Dim P092 As Decimal = P092_IDACIDSecSubtotal
        Dim P098 As Decimal = P098_IDACICSecSubtotal
        Dim P104 As Decimal = P104_IDACIBSecSubtotal
        Dim P110 As Decimal = P110_IDACIASecSubtotal
        Dim P145 As Decimal = P145_EAL1PriSubtotal
        Dim P150 As Decimal = P150_EAL2PriSubtotal
        Dim P155 As Decimal = P155_EAL3PriSubtotal
        Dim P161 As Decimal = P161_EAL1SecSubtotal
        Dim P166 As Decimal = P166_EAL2SecSubtotal
        Dim P171 As Decimal = P171_EAL3SecSubtotal
        Dim P117 As Decimal = P117_LACSubtotal
        Dim P133 As Decimal = P133_PPATotalFunding
        Dim P139 As Decimal = P139_SecPASubtotal
        Dim P177 As Decimal = P177_MobPriSubtotal
        Dim P183 As Decimal = P183_MobSecSubtotal
        Dim P241 As Decimal = P241_PriLumpSumSubtotal
        Dim P245 As Decimal = P245_SecLumpSumSubtotal
        Dim P261 As Decimal = P261_Ex1Subtotal
        Dim AcadFilter As Decimal = F200_SBS_Academies
        Print(P005, "P005", rid)
        Print(P010, "P010", rid)
        Print(P015, "P015", rid)
        Print(P022, "P022", rid)
        Print(P027, "P027", rid)
        Print(P033, "P033", rid)
        Print(P038, "P038", rid)
        Print(P044, "P044", rid)
        Print(P050, "P050", rid)
        Print(P056, "P056", rid)
        Print(P062, "P062", rid)
        Print(P068, "P068", rid)
        Print(P074, "P074", rid)
        Print(P080, "P080", rid)
        Print(P086, "P086", rid)
        Print(P092, "P092", rid)
        Print(P098, "P098", rid)
        Print(P104, "P104", rid)
        Print(P110, "P110", rid)
        Print(P145, "P145", rid)
        Print(P150, "P150", rid)
        Print(P155, "P155", rid)
        Print(P161, "P161", rid)
        Print(P166, "P166", rid)
        Print(P171, "P171", rid)
        Print(P117, "P117", rid)
        Print(P133, "P133", rid)
        Print(P139, "P139", rid)
        Print(P177, "P177", rid)
        Print(P183, "P183", rid)
        Print(P241, "P241", rid)
        Print(P245, "P245", rid)
        Print(P261, "P261", rid)
        If AcadFilter = 1 then
            If F100_AllAcademies = 17181 Or (F100_AllAcademies = 17182 And FundingBasis = 2) Or (F100_AllAcademies = 17183 And FundingBasis = 2) Then
                If string.IsNullOrEmpty(FringeIandAString) Then
                    If LondonFringeCensus < 1 Then
                        LondonFringeCensusTest = 0
                    Else
                        LondonFringeCensusTest = LondonFringeCensus - 1
                    End If

                    Result =(P005 + P010 + P015 + P022 + P027 + P033 + P038 + P044 + P050 + P056 + P062 + P068 + P074 + P080 + P086 + P092 + P098 + P104 + P110 + P145 + P150 + P155 + P161 + P166 + P171 + P117 + P133 + P139 + P177 + P183 + P241 + P245 + P261) * (LondonFringeCensusTest)
                Else
                    If LondonFringeIandA < 1 Then
                        LondonFringeIandATest = 0
                    Else
                        LondonFringeIandATest = LondonFringeIandA - 1
                    End If

                    Result =(P005 + P010 + P015 + P022 + P027 + P033 + P038 + P044 + P050 + P056 + P062 + P068 + P074 + P080 + P086 + P092 + P098 + P104 + P110 + P145 + P150 + P155 + P161 + P166 + P171 + P117 + P133 + P139 + P177 + P183 + P241 + P245 + P261) * (LondonFringeIandATest)
                End if
            ElseIf F100_AllAcademies <> 17181 Then 'And FundingBasis = "Estimate"  Then 
                result = APT_ISB_LondonFringe
            Else
                Exclude(rid)
            End if
        else
            exclude(rid)
        End if

        Print(APT_ISB_LondonFringe, "APT_ISB_LondonFringe", rid)
        Print(F100_AllAcademies, "F100_AllAcademies", rid)
        Print(FundingBasis, "Funding_Basis", rid)
        Print(LondonFringeIandA, "LondonFringeIandA", rid)
        Print(LondonFringeCensus, "LondonFringeCensus", rid)
        Print(LondonFringeIandATest, "LondonFringeIandATest", rid)
        Print(LondonFringeCensusTest, "LondonFringeCensusTest", rid)
        Return result
    End Function

    <Calculation(Id:="2506b128c51241a29918eb9b869e872e")>
    <CalculationSpecification(Id:="P257_InYearFringeSubtotal", Name:="P257_InYearFringeSubtotal")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="41080d8da69046e3a26ff4fb6894eaa8", Name:="Other Factors")>
    Public Function P257_InYearFringeSubtotal As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim P255_FringeSubtotal As Decimal = P255_FringeSubtotal
        Dim P001_1718DaysOpen As Decimal = P001_1718DaysOpen
        Dim Year_Days1718 As Decimal = P025_YearDays_1718
        Dim AcadFilter As Decimal = F200_SBS_Academies
        If AcadFilter = 1 Then
            result = P255_FringeSubtotal * (P001_1718DaysOpen / Year_Days1718)
        Else
            exclude(rid)
        End if

        Print(P255_FringeSubtotal, "P255_FringeSubtotal", rid)
        Print(P001_1718DaysOpen, "P001_1718DaysOpen", rid)
        Print(Year_Days1718, "Year_Days1718", rid)
        Return result
    End Function

    <Calculation(Id:="4eda9bbfb0784f808bf33a7e26c16db4")>
    <CalculationSpecification(Id:="P261_Ex1Subtotal", Name:="P261_Ex1Subtotal")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="41080d8da69046e3a26ff4fb6894eaa8", Name:="Other Factors")>
    Public Function P261_Ex1Subtotal As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim AcadFilter As Decimal = F200_SBS_Academies
        Dim Ex1String As String =
         [Datasets.APTNewISBdataset .1718 ApprovedExceptionalCircumstance1  : _Reserved_for_Additional_lump_sum_for_schools_amalgamated_during__FY16-17
]         Dim ISB_Ex1 As Decimal =
         [Datasets.APTNewISBdataset .1718 ApprovedExceptionalCircumstance1  : _Reserved_for_Additional_lump_sum_for_schools_amalgamated_during__FY16-17
]         If AcadFilter = 1 Then
            If string.IsNullOrEmpty(Ex1String) Then
                result = 0
            Else
                result = ISB_Ex1
            End If
        Else
            exclude(rid)
        End If

        Print(ISB_Ex1, "ISB_Ex1", rid)
        Return result
    End Function

    <Calculation(Id:="87ee1ba60845417b8e1100a57db7b047")>
    <CalculationSpecification(Id:="P262_NSENEx1", Name:="P262_NSENEx1")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="41080d8da69046e3a26ff4fb6894eaa8", Name:="Other Factors")>
    Public Function P262_NSENEx1 As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim AcadFilter As Decimal = F200_SBS_Academies
        Dim P239_PriLumpSumFactor As Decimal = P239_PriLumpSumFactor
        Dim P243_SecLumpSumFactor As Decimal = P243_SecLumpSumFactor
        Dim P247a_NSENLumpSumPri_Percent As Decimal = P247a_NSENLumpSumPri_Percent / 100
        Dim P248a_NSENLumpSumSec_Percent As Decimal = P248a_NSENLumpSumSec_Percent / 100
        Dim P261_Ex1Subtotal As Decimal = P261_Ex1Subtotal
        If AcadFilter = 1 Then
            result =(P261_Ex1Subtotal * P239_PriLumpSumFactor * P247a_NSENLumpSumPri_Percent) + (P261_Ex1Subtotal * P243_SecLumpSumFactor * P248a_NSENLumpSumSec_Percent)
        Else
            exclude(rid)
        End if

        Print(P239_PriLumpSumFactor, "P239_PriLumpSumFactor", rid)
        Print(P243_SecLumpSumFactor, "P243_SecLumpSumFactor", rid)
        Print(P247a_NSENLumpSumPri_Percent, "P247a_NSENLumpSumPri_Percent", rid)
        Print(P248a_NSENLumpSumSec_Percent, "P248a_NSENLumpSumSec_Percent", rid)
        Print(P261_Ex1Subtotal, "P261_Ex1Subtotal", rid)
        Return result
    End Function

    <Calculation(Id:="2268eaf4cbee4eca874f23853dcef809")>
    <CalculationSpecification(Id:="P262a_NSENEx1_Percent", Name:="P262a_NSENEx1_Percent")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="41080d8da69046e3a26ff4fb6894eaa8", Name:="Other Factors")>
    Public Function P262a_NSENEx1_Percent As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="4fa5257d2f5943f3a078dab7da804699")>
    <CalculationSpecification(Id:="P264_InYearEx1Subtotal", Name:="P264_InYearEx1Subtotal")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="41080d8da69046e3a26ff4fb6894eaa8", Name:="Other Factors")>
    Public Function P264_InYearEx1Subtotal As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim P261_Ex1Subtotal As Decimal = P261_Ex1Subtotal
        Dim P001_1718DaysOpen As Decimal = P001_1718DaysOpen
        Dim Year_Days1718 As Decimal = P025_YearDays_1718
        Dim AcadFilter As Decimal = F200_SBS_Academies
        If AcadFilter = 1 Then
            result = P261_Ex1Subtotal * (P001_1718DaysOpen / Year_Days1718)
        Else
            exclude(rid)
        End if

        Print(P261_Ex1Subtotal, "P261_Ex1Subtotal", rid)
        Print(P001_1718DaysOpen, "P001_1718DaysOpen", rid)
        Print(Year_Days1718, "Year_Days1718", rid)
        Return result
    End Function

    <Calculation(Id:="9cbc940fd5554096871e3ca1432b182f")>
    <CalculationSpecification(Id:="P265_Ex2Subtotal", Name:="P265_Ex2Subtotal")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="41080d8da69046e3a26ff4fb6894eaa8", Name:="Other Factors")>
    Public Function P265_Ex2Subtotal As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim AcadFilter As Decimal = F200_SBS_Academies
        Dim Ex2String As String =
         [Datasets.APTNewISBdataset .17 – 18 ApprovedExceptionalCircumstance2  : _Reserved_for_additional_sparsity_lump_sum ] 
        Dim ISB_Ex2 As Decimal =
         [Datasets.APTNewISBdataset .17 – 18 ApprovedExceptionalCircumstance2  : _Reserved_for_additional_sparsity_lump_sum ] 
        If AcadFilter = 1 Then
            If string.IsNullOrEmpty(Ex2String) Then
                result = 0
            Else
                result = ISB_Ex2
            End If
        Else
            exclude(rid)
        End If

        Print(ISB_Ex2, "ISB_Ex2", rid)
        Return result
    End Function

    <Calculation(Id:="75ae74b2aee54105afe2e6da7d8fea8e")>
    <CalculationSpecification(Id:="P266_NSENEx2", Name:="P266_NSENEx2")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="41080d8da69046e3a26ff4fb6894eaa8", Name:="Other Factors")>
    Public Function P266_NSENEx2 As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim P265_Ex2Subtotal As Decimal = P265_Ex2Subtotal
        Dim P266a_NSENEx2_Percent As Decimal = P266a_NSENEx2_Percent / 100
        Dim AcadFilter As Decimal = F200_SBS_Academies
        If AcadFilter = 1 Then
            result = P265_Ex2Subtotal * P266a_NSENEx2_Percent
        Else
            exclude(rid)
        End if

        Print(P265_Ex2Subtotal, "P265_Ex2Subtotal", rid)
        Print(P266a_NSENEx2_Percent, "P266a_NSENEx2_Percent", rid)
        Return result
    End Function

    <Calculation(Id:="d986a8e1e7214234a626fce3f8f6175a")>
    <CalculationSpecification(Id:="P266a_NSENEx2_Percent", Name:="P266a_NSENEx2_Percent")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="41080d8da69046e3a26ff4fb6894eaa8", Name:="Other Factors")>
    Public Function P266a_NSENEx2_Percent As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim Notional_SEN As Decimal = LAtoProv(Datasets.APTProformadataset.ExceptionalCircumstance2NotionalSEN)
        Dim AcadFilter As Decimal = F200_SBS_Academies
        If AcadFilter = 1 then
            result = Notional_SEN * 100
        Else
            exclude(rid)
        End if

        Print(Notional_SEN, "Notional_SEN", rid)
        Return result
    End Function

    <Calculation(Id:="7563d9c7ced64e798cfcbaef796c4706")>
    <CalculationSpecification(Id:="P267_InYearEx2Subtotal", Name:="P267_InYearEx2Subtotal")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="41080d8da69046e3a26ff4fb6894eaa8", Name:="Other Factors")>
    Public Function P267_InYearEx2Subtotal As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim P265_Ex2Subtotal As Decimal = P265_Ex2Subtotal
        Dim P001_1718DaysOpen As Decimal = P001_1718DaysOpen
        Dim Year_Days1718 As Decimal = P025_YearDays_1718
        Dim AcadFilter As Decimal = F200_SBS_Academies
        If AcadFilter = 1 Then
            result = P265_Ex2Subtotal * (P001_1718DaysOpen / Year_Days1718)
        Else
            exclude(rid)
        End if

        Print(P265_Ex2Subtotal, "P265_Ex2Subtotal", rid)
        Print(P001_1718DaysOpen, "P001_1718DaysOpen", rid)
        Print(Year_Days1718, "Year_Days1718", rid)
        Return result
    End Function

    <Calculation(Id:="5c1b174eca06422db5af2268dd5ce7a4")>
    <CalculationSpecification(Id:="P269_Ex3Subtotal", Name:="P269_Ex3Subtotal")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="41080d8da69046e3a26ff4fb6894eaa8", Name:="Other Factors")>
    Public Function P269_Ex3Subtotal As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim AcadFilter As Decimal = F200_SBS_Academies
        Dim Ex3String As Decimal = Datasets.APTNewISBdataset .17 – 18 ApprovedExceptionalCircumstance3 
        Dim ISB_Ex3 As Decimal = Datasets.APTNewISBdataset .17 – 18 ApprovedExceptionalCircumstance3 
        If AcadFilter = 1 Then
            If string.IsNullOrEmpty(Ex3String) Then
                result = 0
            Else
                result = ISB_Ex3
            End If
        Else
            exclude(rid)
        End If

        Print(ISB_Ex3, "ISB_Ex3", rid)
        Return result
    End Function

    <Calculation(Id:="af07d29db4794ad58be9aa96042a762e")>
    <CalculationSpecification(Id:="P270_NSENEx3", Name:="P270_NSENEx3")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="41080d8da69046e3a26ff4fb6894eaa8", Name:="Other Factors")>
    Public Function P270_NSENEx3 As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim P269_Ex3Subtotal As Decimal = P269_Ex3Subtotal
        Dim P270a_NSENEx3_Percent As Decimal = P270a_NSENEx3Percent / 100
        Dim AcadFilter As Decimal = F200_SBS_Academies
        If AcadFilter = 1 Then
            result = P269_Ex3Subtotal * P270a_NSENEx3_Percent
        Else
            exclude(rid)
        End if

        Print(P269_Ex3Subtotal, "P269_Ex3Subtotal", rid)
        Print(P270a_NSENEx3_Percent, "P270a_NSENEx3_Percent", rid)
        Return result
    End Function

    <Calculation(Id:="65bca997e2ce4a21a90483a30570c79a")>
    <CalculationSpecification(Id:="P270a_NSENEx3_Percent", Name:="P270a_NSENEx3_Percent")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="41080d8da69046e3a26ff4fb6894eaa8", Name:="Other Factors")>
    Public Function P270a_NSENEx3_Percent As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="6809a2470ff14f92a293a99ef6ee84ca")>
    <CalculationSpecification(Id:="P271_InYearEx3Subtotal", Name:="P271_InYearEx3Subtotal")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="41080d8da69046e3a26ff4fb6894eaa8", Name:="Other Factors")>
    Public Function P271_InYearEx3Subtotal As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim P269_Ex3Subtotal As Decimal = P269_Ex3Subtotal
        Dim P001_1718DaysOpen As Decimal = P001_1718DaysOpen
        Dim Year_Days1718 As Decimal = P025_YearDays_1718
        Dim AcadFilter As Decimal = F200_SBS_Academies
        If AcadFilter = 1 Then
            result = P269_Ex3Subtotal * (P001_1718DaysOpen / Year_Days1718)
        Else
            exclude(rid)
        End if

        Print(P269_Ex3Subtotal, "P269_Ex3Subtotal", rid)
        Print(P001_1718DaysOpen, "P001_1718DaysOpen", rid)
        Print(Year_Days1718, "Year_Days1718", rid)
        Return result
    End Function

    <Calculation(Id:="741aad6423574b3faf96e7c3027e9160")>
    <CalculationSpecification(Id:="P273_Ex4Subtotal", Name:="P273_Ex4Subtotal")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="41080d8da69046e3a26ff4fb6894eaa8", Name:="Other Factors")>
    Public Function P273_Ex4Subtotal As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim AcadFilter As Decimal = F200_SBS_Academies
        Dim Ex4String As Decimal = Datasets.APTNewISBdataset .17 – 18 ApprovedExceptionalCircumstance4 
        Dim ISB_Ex4 As Decimal = Datasets.APTNewISBdataset .17 – 18 ApprovedExceptionalCircumstance4 
        If AcadFilter = 1 Then
            If string.IsNullOrEmpty(Ex4String) Then
                result = 0
            Else
                result = ISB_Ex4
            End If
        Else
            exclude(rid)
        End If

        Print(ISB_Ex4, "ISB_Ex4", rid)
        Return result
    End Function

    <Calculation(Id:="47a196ad6a454e94914d2b6cbb2de4f6")>
    <CalculationSpecification(Id:="P274_NSENEx4", Name:="P274_NSENEx4")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="41080d8da69046e3a26ff4fb6894eaa8", Name:="Other Factors")>
    Public Function P274_NSENEx4 As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim AcadFilter As Decimal = F200_SBS_Academies
        Dim Ex4String As String = Datasets.APTNewISBdataset .17 – 18 ApprovedExceptionalCircumstance4 
        Dim ISB_Ex4 As Decimal = Datasets.APTNewISBdataset .17 – 18 ApprovedExceptionalCircumstance4 
        Dim Ex4NSEN As Decimal = P274a_NSENEx4_Percent / 100
        If AcadFilter = 1 Then
            If string.IsNullOrEmpty(Ex4String) Then
                result = 0
            Else
                result = ISB_Ex4 * EX4NSEN
            End If
        Else
            exclude(rid)
        End If

        Print(ISB_Ex4, "ISB_Ex4", rid)
        Return result
    End Function

    <Calculation(Id:="677867fb9258464a8f97e149e069ed1a")>
    <CalculationSpecification(Id:="P274a_NSENEx4_Percent", Name:="P274a_NSENEx4_Percent")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="41080d8da69046e3a26ff4fb6894eaa8", Name:="Other Factors")>
    Public Function P274a_NSENEx4_Percent As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim Notional_SEN As Decimal = LAtoProv(Datasets.APTProformadataset.ExceptionalCircumstance4NotionalSEN)
        Dim AcadFilter As Decimal = F200_SBS_Academies
        If AcadFilter = 1 then
            result = Notional_SEN * 100
        Else
            exclude(rid)
        End if

        Print(Notional_SEN, "Notional_SEN", rid)
        Return result
    End Function

    <Calculation(Id:="51026e4074544f59b971f10f993475eb")>
    <CalculationSpecification(Id:="P275_InYearEx4Subtotal", Name:="P275_InYearEx4Subtotal")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="41080d8da69046e3a26ff4fb6894eaa8", Name:="Other Factors")>
    Public Function P275_InYearEx4Subtotal As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="21bdb02fb26741b1b1c01ef02d115c71")>
    <CalculationSpecification(Id:="P277_Ex5Subtotal", Name:="P277_Ex5Subtotal")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="41080d8da69046e3a26ff4fb6894eaa8", Name:="Other Factors")>
    Public Function P277_Ex5Subtotal As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim AcadFilter As Decimal = F200_SBS_Academies
        Dim Ex5String As String = Datasets.APTNewISBdataset .17 – 18 ApprovedExceptionalCircumstance5 
        Dim ISB_Ex5 As Decimal = Datasets.APTNewISBdataset .17 – 18 ApprovedExceptionalCircumstance5 
        If AcadFilter = 1 Then
            If string.IsNullOrEmpty(Ex5String) Then
                result = 0
            Else
                result = ISB_Ex5
            End If
        Else
            exclude(rid)
        End If

        Print(ISB_Ex5, "ISB_Ex5", rid)
        Return result
    End Function

    <Calculation(Id:="956d160201364b8684a4fd7e04a1769c")>
    <CalculationSpecification(Id:="P278_NSENEx5", Name:="P278_NSENEx5")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="41080d8da69046e3a26ff4fb6894eaa8", Name:="Other Factors")>
    Public Function P278_NSENEx5 As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim P277_Ex5Subtotal As Decimal = P277_Ex5Subtotal
        Dim P278a_NSENEx5_Percent As Decimal = P278a_NSENEx5_Percent / 100
        Dim AcadFilter As Decimal = F200_SBS_Academies
        If AcadFilter = 1 Then
            result = P277_Ex5Subtotal * P278a_NSENEx5_Percent
        Else
            exclude(rid)
        End if

        Print(P277_Ex5Subtotal, "P277_Ex5Subtotal", rid)
        Print(P278a_NSENEx5_Percent, "P278a_NSENEx5_Percent", rid)
        Return result
    End Function

    <Calculation(Id:="8ae43c7d0bba4513a932e3fc2d868200")>
    <CalculationSpecification(Id:="P278a_NSENEx5_Percent", Name:="P278a_NSENEx5_Percent")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="41080d8da69046e3a26ff4fb6894eaa8", Name:="Other Factors")>
    Public Function P278a_NSENEx5_Percent As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim Notional_SEN As Decimal = LAtoProv(Datasets.APTProformadataset.ExceptionalCircumstance5NotionalSEN)
        Dim AcadFilter As Decimal = F200_SBS_Academies
        If AcadFilter = 1 then
            result = Notional_SEN * 100
        Else
            exclude(rid)
        End if

        Print(Notional_SEN, "Notional_SEN", rid)
        Return result
    End Function

    <Calculation(Id:="714db9ff9c7845f8b4cc2c06cc5511d9")>
    <CalculationSpecification(Id:="P279_InYearEx5Subtotal", Name:="P279_InYearEx5Subtotal")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="41080d8da69046e3a26ff4fb6894eaa8", Name:="Other Factors")>
    Public Function P279_InYearEx5Subtotal As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim P277_Ex5Subtotal As Decimal = P277_Ex5Subtotal
        Dim P001_1718DaysOpen As Decimal = P001_1718DaysOpen
        Dim Year_Days1718 As Decimal = P025_YearDays_1718
        Dim AcadFilter As Decimal = F200_SBS_Academies
        If AcadFilter = 1 Then
            result = P277_Ex5Subtotal * (P001_1718DaysOpen / Year_Days1718)
        Else
            exclude(rid)
        End if

        Print(P277_Ex5Subtotal, "P277_Ex5Subtotal", rid)
        Print(P001_1718DaysOpen, "P001_1718DaysOpen", rid)
        Print(Year_Days1718, "Year_Days1718", rid)
        Return result
    End Function

    <Calculation(Id:="51738c7b847d44d8ada242690a470073")>
    <CalculationSpecification(Id:="P281_Ex6Subtotal", Name:="P281_Ex6Subtotal")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="41080d8da69046e3a26ff4fb6894eaa8", Name:="Other Factors")>
    Public Function P281_Ex6Subtotal As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim AcadFilter As Decimal = F200_SBS_Academies
        Dim Ex6String As String = Datasets.APTNewISBdataset .17 – 18 ApprovedExceptionalCircumstance6 
        Dim ISB_Ex6 As Decimal = Datasets.APTNewISBdataset .17 – 18 ApprovedExceptionalCircumstance6 
        Dim SBSP286 As Decimal = P286_PriorYearAdjustmentSubtotal
        If AcadFilter = 1 Then
            If string.IsNullOrEmpty(Ex6String) Then
                result = SBSP286
            Else
                result = ISB_Ex6 + SBSP286
            End If
        Else
            exclude(rid)
        End If

        Print(ISB_Ex6, "ISB_Ex6", rid)
        Return result
    End Function

    <Calculation(Id:="eb995bb071c24aa3a326596182336964")>
    <CalculationSpecification(Id:="P282_NSENEx6", Name:="P282_NSENEx6")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="41080d8da69046e3a26ff4fb6894eaa8", Name:="Other Factors")>
    Public Function P282_NSENEx6 As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim P281_Ex6Subtotal As Decimal = P281_Ex6Subtotal
        Dim P282a_NSENEx6_Percent As Decimal = P282a_NSENEx6_Percent / 100
        Dim AcadFilter As Decimal = F200_SBS_Academies
        Dim SBSP286 As Decimal = P286_PriorYearAdjustmentSubtotal
        If AcadFilter = 1 Then
            result =(P281_Ex6Subtotal - SBSP286) * P282a_NSENEx6_Percent
        Else
            exclude(rid)
        End if

        Print(P281_Ex6Subtotal, "P281_Ex6Subtotal", rid)
        Print(P282a_NSENEx6_Percent, "P282a_NSENEx6_Percent", rid)
        Return result
    End Function

    <Calculation(Id:="65a82c765f024a6d92a53108746236e2")>
    <CalculationSpecification(Id:="P282a_NSENEx6_Percent", Name:="P282a_NSENEx6_Percent")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="41080d8da69046e3a26ff4fb6894eaa8", Name:="Other Factors")>
    Public Function P282a_NSENEx6_Percent As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim Notional_SEN As Decimal = LAtoProv(Datasets.APTProformadataset.ExceptionalCircumstance6NotionalSEN)
        Dim AcadFilter As Decimal = F200_SBS_Academies
        If AcadFilter = 1 then
            result = Notional_SEN * 100
        Else
            exclude(rid)
        End if

        Print(Notional_SEN, "Notional_SEN", rid)
        Return result
    End Function

    <Calculation(Id:="572458fea7654593a0485862b5061204")>
    <CalculationSpecification(Id:="P283_InYearEx6Subtotal", Name:="P283_InYearEx6Subtotal")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="41080d8da69046e3a26ff4fb6894eaa8", Name:="Other Factors")>
    Public Function P283_InYearEx6Subtotal As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim P281_Ex6Subtotal As Decimal = P281_Ex6Subtotal
        Dim P001_1718DaysOpen As Decimal = P001_1718DaysOpen
        Dim Year_Days1718 As Decimal = P025_YearDays_1718
        Dim AcadFilter As Decimal = F200_SBS_Academies
        If AcadFilter = 1 Then
            result = P281_Ex6Subtotal * (P001_1718DaysOpen / Year_Days1718)
        Else
            exclude(rid)
        End if

        Print(P281_Ex6Subtotal, "P281_Ex6Subtotal", rid)
        Print(P001_1718DaysOpen, "P001_1718DaysOpen", rid)
        Print(Year_Days1718, "Year_Days1718", rid)
        Return result
    End Function

    <Calculation(Id:="2f63de878e364251aa4ee0773ae454f4")>
    <CalculationSpecification(Id:="P284_NSENSubtotal", Name:="P284_NSENSubtotal")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="41080d8da69046e3a26ff4fb6894eaa8", Name:="Other Factors")>
    Public Function P284_NSENSubtotal As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim APT_NSENSubtotal As Decimal = Datasets.APTNewISBdataset.NotionalSENBudget
        Dim F100_AllAcademies As Decimal = F100_AllAcademies
        Dim FundingBasis As Decimal = F900_FundingBasis
        Dim LondonFringeIandA As Decimal = Datasets.APTInputsandAdjustments.LondonFringe
        Dim LondonFringeIandAString As String = Datasets.APTInputsandAdjustments.LondonFringe
        Dim LondonFringeCensus As Decimal = Datasets.CensusPupilCharacteristics.LondonFringe
        Dim LondonFringeCensusString As String = Datasets.CensusPupilCharacteristics.LondonFringe
        Dim Fringe As Decimal = 0
        Dim P006 As Decimal = P006_NSEN_PriBE
        Dim P011 As Decimal = P011_NSEN_KS3BE_percent
        Dim P016 As Decimal = P016_NSEN_KS4BE
        Dim P028 As Decimal = P028_NSENFSMPri
        Dim P039 As Decimal = P039_NSENFSMSec
        Dim P045 As Decimal = P045_NSENIDACIFPri
        Dim P051 As Decimal = P051_NSENIDACIEPri
        Dim P057 As Decimal = P057_NSENIDACIDPri
        Dim P063 As Decimal = P063_NSENIDACICPri
        Dim P069 As Decimal = P069_NSENIDACIBPri
        Dim P075 As Decimal = P075_NSENIDACIAPri
        Dim P081 As Decimal = P081_NSENIDACIFSec
        Dim P087 As Decimal = P087_NSENIDACIESec
        Dim P093 As Decimal = P093_NSENIDACIDSec
        Dim P099 As Decimal = P099_NSENIDACICSec
        Dim P105 As Decimal = P105_NSENIDACIBSec
        Dim P111 As Decimal = P111_NSENIDACIASec
        Dim P156 As Decimal = P156_NSENPriEAL
        Dim P172 As Decimal = P172_NSENSecEAL
        Dim P178 As Decimal = P178_NSENMobPri
        Dim P184 As Decimal = P184_NSENMobSec
        Dim P118 As Decimal = P118_NSENLAC
        Dim P134 As Decimal = P134_NSENPPA
        Dim P140 As Decimal = P140_NSENSecPA
        Dim P236 As Decimal = P236_NSENSparsity
        Dim P247 As Decimal = P247_NSENLumpSumPri
        Dim P248 As Decimal = P248_NSENLumpSumSec
        Dim P250 As Decimal = P250_NSENSplitSites
        Dim P253 As Decimal = P253_NSENPFI
        Dim P262 As Decimal = P262_NSENEx1
        Dim P266 As Decimal = P266_NSENEx2
        Dim P270 As Decimal = P270_NSENEx3
        Dim P274 As Decimal = P274_NSENEx4
        Dim P278 As Decimal = P278_NSENEx5
        Dim P282 As Decimal = P282_NSENEx6
        Dim F200 As Decimal = F200_SBS_Academies
        If F200 = 1 then
            If F100_AllAcademies = 17181 Or (F100_AllAcademies = 17182 And FundingBasis = 2) Or (F100_AllAcademies = 17183 And FundingBasis = 2) Then
                If string.IsNullOrEmpty(LondonFringeIandAString) And string.IsNullOrEmpty(LondonFringeCensusString) Then
                    Fringe = 1
                ElseIf string.IsNullOrEmpty(LondonFringeIandAString) Then
                    Fringe = LondonFringeCensus
                Else
                    Fringe = LondonFringeIandA
                End If

                Result =(Fringe * (P006 + P011 + P016 + P028 + P039 + P045 + P051 + P057 + P063 + P069 + P075 + P081 + P087 + P093 + P099 + P105 + P111 + P118 + P134 + P140 + P156 + P172 + P178 + P184 + P236 + P247 + P248 + P262)) + P250 + P253 + P266 + P270 + P274 + P278 + P282
            ElseIf(F100_AllAcademies = 17182 Or F100_AllAcademies = 17183) And FundingBasis = 1 Then
                result = APT_NSENSubtotal
            Else
                Exclude(rid)
            End If
        Else
            exclude(rid)
        End if

        Return result
    End Function

    <Calculation(Id:="7ccbeeae8fef4570ae1461ede6ad63c0")>
    <CalculationSpecification(Id:="P285_InYearNSENSubtotal", Name:="P285_InYearNSENSubtotal")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="41080d8da69046e3a26ff4fb6894eaa8", Name:="Other Factors")>
    Public Function P285_InYearNSENSubtotal As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim P284_NSENSubtotal As Decimal = P284_NSENSubtotal
        Dim P001_1718DaysOpen As Decimal = P001_1718DaysOpen
        Dim Year_Days1718 As Decimal = P025_YearDays_1718
        Dim AcadFilter As Decimal = F200_SBS_Academies
        If AcadFilter = 1 Then
            result = P284_NSENSubtotal * (P001_1718DaysOpen / Year_Days1718)
        Else
            exclude(rid)
        End if

        Print(P284_NSENSubtotal, "P284_NSENSubtotal", rid)
        Print(P001_1718DaysOpen, "P001_1718DaysOpen", rid)
        Print(Year_Days1718, "Year_Days1718", rid)
        Return result
    End Function

    <Calculation(Id:="f0a21e6b3f9240bdbdd50d67bfc42b39")>
    <CalculationSpecification(Id:="P286_PriorYearAdjustmentSubtotal", Name:="P286_PriorYearAdjustmentSubtotal")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="41080d8da69046e3a26ff4fb6894eaa8", Name:="Other Factors")>
    Public Function P286_PriorYearAdjustmentSubtotal As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim Adjustments As Decimal = Datasets.EFAAdjustments.APTAdjustmentrelatingtoprioryearSBS
        Dim AcadFilter As Decimal = F200_SBS_Academies
        If AcadFilter = 1 Then
            result = Adjustments
        Else
            exclude(rid)
        End if

        Print(Adjustments, "Adjustments", rid)
        Return result
    End Function

    <Calculation(Id:="e4e210b99ebd420dbed00395a88068c1")>
    <CalculationSpecification(Id:="P287_InYearPriorYearAdjsutmentSubtotal", Name:="P287_InYearPriorYearAdjsutmentSubtotal")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="41080d8da69046e3a26ff4fb6894eaa8", Name:="Other Factors")>
    Public Function P287_InYearPriorYearAdjsutmentSubtotal As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="beb41e53c2b24bb4a7d1b51cab279562")>
    <CalculationSpecification(Id:="P298_Growth", Name:="P298_Growth")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="41080d8da69046e3a26ff4fb6894eaa8", Name:="Other Factors")>
    Public Function P298_Growth As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim P298Growth As Decimal = Datasets.EFAAdjustments.GrowthFundingTotal
        Dim P298Growthstr As string = Datasets.EFAAdjustments.GrowthFundingTotal
        Dim F200 As Decimal = F200_SBS_Academies
        If F200 = 1 then
            If string.IsNullOrEmpty(P298GrowthStr) then
                result = 0
            else
                result = P298Growth
            End if
        Else
            exclude(rid)
        End if

        Return result
    End Function

    <Calculation(Id:="8a9f6bdce36f4a8da969aa9e607222a3")>
    <CalculationSpecification(Id:="P299_InYearGrowth", Name:="P299_InYearGrowth")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="41080d8da69046e3a26ff4fb6894eaa8", Name:="Other Factors")>
    Public Function P299_InYearGrowth As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim P298Growth As Decimal = P298_Growth
        Dim P001_1718DaysOpen As Decimal = P001_1718DaysOpen
        Dim Year_Days1718 As Decimal = P025_YearDays_1718
        Dim AcadFilter As Decimal = F200_SBS_Academies
        If AcadFilter = 1 Then
            result = P298Growth * (P001_1718DaysOpen / Year_Days1718)
        Else
            exclude(rid)
        End if

        Print(P298Growth, "P286_PriorYearAdjustmentSubtotal", rid)
        Print(P001_1718DaysOpen, "P001_1718DaysOpen", rid)
        Print(Year_Days1718, "Year_Days1718", rid)
        Return result
    End Function

    <Calculation(Id:="d258897fa1494725b2479b4cc61728f4")>
    <CalculationSpecification(Id:="P300_SBSOutcomeAdjustment", Name:="P300_SBSOutcomeAdjustment")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="41080d8da69046e3a26ff4fb6894eaa8", Name:="Other Factors")>
    Public Function P300_SBSOutcomeAdjustment As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim P300SBSAdj As Decimal = Datasets.EFAAdjustments.SBSoutcomeadjustment
        Dim P300SBSAdjStr As String = Datasets.EFAAdjustments.SBSoutcomeadjustment
        Dim F200 As Decimal = F200_SBS_Academies
        If F200 = 1 then
            If string.IsNullOrEmpty(P300SBSAdjStr) Then
                result = 0
            else
                result = P300SBSAdj
            End if
        else
            exclude(rid)
        End if

        Return result
    End Function

    <Calculation(Id:="c61c227697c74e3da9c0bbff231798b2")>
    <CalculationSpecification(Id:="P301_InYearSBSOutcomeAdjustment", Name:="P301_InYearSBSOutcomeAdjustment")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="41080d8da69046e3a26ff4fb6894eaa8", Name:="Other Factors")>
    Public Function P301_InYearSBSOutcomeAdjustment As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim P300SBSAdj As Decimal = P300_SBSOutcomeAdjustment
        Dim P001_1718DaysOpen As Decimal = P001_1718DaysOpen
        Dim Year_Days1718 As Decimal = P025_YearDays_1718
        Dim AcadFilter As Decimal = F200_SBS_Academies
        If AcadFilter = 1 Then
            result = P300SBSAdj * (P001_1718DaysOpen / Year_Days1718)
        Else
            exclude(rid)
        End if

        Print(P300SBSAdj, "P286_PriorYearAdjustmentSubtotal", rid)
        Print(P001_1718DaysOpen, "P001_1718DaysOpen", rid)
        Print(Year_Days1718, "Year_Days1718", rid)
        Return result
    End Function

    <Calculation(Id:="f754e9f67dd6459aa9b1547fa2bded77")>
    <CalculationSpecification(Id:="P120_PPAindicator", Name:="P120_PPAindicator")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="c6b57f57424c494fb0ed394ffb1af34c", Name:="Prior Attainment")>
    Public Function P120_PPAindicator As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="264a36ab57784c2ab575cfed16f7f5be")>
    <CalculationSpecification(Id:="P121_PPAY5to6Proportion73", Name:="P121_PPAY5to6Proportion73")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="c6b57f57424c494fb0ed394ffb1af34c", Name:="Prior Attainment")>
    Public Function P121_PPAY5to6Proportion73 As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim F100_AllAcademies As Decimal = F100_AllAcademies
        Dim FundingBasis As String = F900_FundingBasis
        Dim F200_SBS_Academies As Decimal = F200_SBS_Academies
        Dim LowAttP73PupilCharac_String As String = Datasets.CensusPupilCharacteristics.LowAttainnmentunderoldEYFSPproportion73
        Dim LowAttPrimaryP73Adj_String As String = Datasets.APTInputsandAdjustments.LowAttainmentunderoldFSPProportion73
        Dim LAAV_Y25P73 As Decimal = LaToProv(Datasets.LocalAuthorityAverages.LowAttainmentunderoldEYFSPProportion73)
        Dim PupilCharacCensusP73 As Decimal = Datasets.CensusPupilCharacteristics.LowAttainnmentunderoldEYFSPproportion73
        Dim AdjP73 As Decimal = Datasets.APTInputsandAdjustments.LowAttainmentunderoldFSPProportion73
        If F200_SBS_Academies = 1 then
            If string.IsNullOrEmpty(LowAttP73PupilCharac_String) And String.IsNullOrEmpty(LowAttPrimaryP73Adj_String) then
                Result = LAAV_Y25P73
            Else
                If String.IsNullOrEmpty(LowAttPrimaryP73Adj_String) then
                    Result = PupilCharacCensusP73
                Else
                    Result = AdjP73
                End If
            End If
        Else
            exclude(rid)
        End If

        Print(F100_AllAcademies, "F100_AllAcademies", rid)
        Print(FundingBasis, "FundingBasis", rid)
        Print(F200_SBS_Academies, "F200_SBS_Academies", rid)
        Print(LowAttP73PupilCharac_String, "LowAttP73PupilCharac_String", rid)
        Print(LowAttPrimaryP73Adj_String, "LowAttPrimaryP73Adj_String", rid)
        Print(LAAV_Y25P73, "LAAV_Y25P73", rid)
        Print(PupilCharacCensusP73, "PupilCharacCensusP73", rid)
        Print(AdjP73, "AdjP73", rid)
        Return result
    End Function

    <Calculation(Id:="4eb5c82aea9145ac85cb8af91c5e6e53")>
    <CalculationSpecification(Id:="P122_PPAY5to6Proportion78", Name:="P122_PPAY5to6Proportion78")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="c6b57f57424c494fb0ed394ffb1af34c", Name:="Prior Attainment")>
    Public Function P122_PPAY5to6Proportion78 As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim F100_AllAcademies As Decimal = F100_AllAcademies
        Dim FundingBasis As String = F900_FundingBasis
        Dim F200_SBS_Academies As Decimal = F200_SBS_Academies
        Dim LowAttP78PupilCharac_String As String = Datasets.CensusPupilCharacteristics.LowAttainnmentunderoldEYFSPproportion78
        Dim LowAttPrimaryP78Adj_String As String = Datasets.APTInputsandAdjustments.LowAttainmentunderoldFSPProportion78
        Dim LAAV_Y25P78 As Decimal = LaToProv(Datasets.LocalAuthorityAverages.LowAttainmentunderoldEYFSPProportion78)
        Dim PupilCharacCensusP78 As Decimal = Datasets.CensusPupilCharacteristics.LowAttainnmentunderoldEYFSPproportion78
        Dim AdjP78 As Decimal = Datasets.APTInputsandAdjustments.LowAttainmentunderoldFSPProportion78
        If F200_SBS_Academies = 1 then
            If string.IsNullOrEmpty(LowAttP78PupilCharac_String) And String.IsNullOrEmpty(LowAttPrimaryP78Adj_String) then
                Result = LAAV_Y25P78
            Else
                If String.IsNullOrEmpty(LowAttPrimaryP78Adj_String) then
                    Result = PupilCharacCensusP78
                Else
                    Result = AdjP78
                End If
            End If
        Else
            exclude(rid)
        End If

        Print(F100_AllAcademies, "F100_AllAcademies", rid)
        Print(FundingBasis, "FundingBasis", rid)
        Print(F200_SBS_Academies, "F200_SBS_Academies", rid)
        Print(LowAttP78PupilCharac_String, "LowAttP78PupilCharac_String", rid)
        Print(LowAttPrimaryP78Adj_String, "LowAttPrimaryP78Adj_String", rid)
        Print(LAAV_Y25P78, "LAAV_Y25P78", rid)
        Print(PupilCharacCensusP78, "PupilCharacCensusP78", rid)
        Print(AdjP78, "AdjP78", rid)
        Return result
    End Function

    <Calculation(Id:="c967c9dbbc3340aa8e1503ecc3e045a3")>
    <CalculationSpecification(Id:="P122a_PPAY7378forFAPOnly", Name:="P122a_PPAY7378forFAPOnly")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="c6b57f57424c494fb0ed394ffb1af34c", Name:="Prior Attainment")>
    Public Function P122a_PPAY7378forFAPOnly As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="2373b1422804466897cff3988c31cc7b")>
    <CalculationSpecification(Id:="P123_PPAY1to4ProportionUnder", Name:="P123_PPAY1to4ProportionUnder")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="c6b57f57424c494fb0ed394ffb1af34c", Name:="Prior Attainment")>
    Public Function P123_PPAY1to4ProportionUnder As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim F100_AllAcademies As Decimal = F100_AllAcademies
        Dim FundingBasis As String = F900_FUndingBasis
        Dim F200_SBS_Academies As Decimal = F200_SBS_Academies
        Dim LowAttPEYPupilCharac_String As String = Datasets.CensusPupilCharacteristics.LowAttainmentProportionundernewEYFSP
        Dim LowAttPrimaryPEYAdj_String As String = Datasets.APTInputsandAdjustments.LowattainmentProportionundernewEYFSP
        Dim LAAV_PEY As Decimal = LaToProv(Datasets.LocalAuthorityAverages.LowattainmentProportionundernewEYFSP)
        Dim PupilCharacCensusPEY As Decimal = Datasets.CensusPupilCharacteristics.LowattainmentProportionundernewEYFSP
        Dim AdjPEY As Decimal = Datasets.APTInputsandAdjustments.LowattainmentProportionundernewEYFSP
        If F200_SBS_Academies = 1 then
            If string.IsNullOrEmpty(LowAttPEYPupilCharac_String) And String.IsNullOrEmpty(LowAttPrimaryPEYAdj_String) then
                Result = LAAV_PEY
            Else
                If String.IsNullOrEmpty(LowAttPrimaryPEYAdj_String) then
                    Result = PupilCharacCensusPEY
                Else
                    Result = AdjPEY
                End If
            End If
        Else
            exclude(rid)
        End If

        Print(F100_AllAcademies, "F100_AllAcademies", rid)
        Print(FundingBasis, "FundingBasis", rid)
        Print(F200_SBS_Academies, "F200_SBS_Academies", rid)
        Print(LowAttPEYPupilCharac_String, "LowAttPEYPupilCharac_String", rid)
        Print(LowAttPrimaryPEYAdj_String, "LowAttPrimaryPEYAdj_String", rid)
        Print(LAAV_PEY, "LAAV_YPEY", rid)
        Print(PupilCharacCensusPEY, "PupilCharacCensusPEY", rid)
        Print(AdjPEY, "AdjPEY", rid)
        Return result
    End Function

    <Calculation(Id:="2597e18a2e4841acbce6937574ec5782")>
    <CalculationSpecification(Id:="P124_PPAY5to6NOR", Name:="P124_PPAY5to6NOR")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="c6b57f57424c494fb0ed394ffb1af34c", Name:="Prior Attainment")>
    Public Function P124_PPAY5to6NOR As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim F100_AllAcademies As Decimal = F100_AllAcademies
        Dim FundingBasis As Decimal = F900_FundingBasis
        Dim F200_SBS_Academies As Decimal = F200_SBS_Academies
        Dim NOR_P04_Y5Y6 As Decimal = NOR_P04_Y5Y6
        If F200_SBS_Academies = 1 then
            Result = NOR_P04_Y5Y6
        Else
            Exclude(rid)
        End iF

        Print(F100_AllAcademies, "F100_AllAcademies", rid)
        Print(FundingBasis, "FundingBasis", rid)
        Print(F200_SBS_Academies, "F200_SBS_Academies", rid)
        Print(NOR_P04_Y5Y6, "NOR_04_Y5Y6", rid)
        Return result
    End Function

    <Calculation(Id:="175361f3e3a240e4a2ee44148ca1f82c")>
    <CalculationSpecification(Id:="P125_PPAY1to4NOR", Name:="P125_PPAY1to4NOR")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="c6b57f57424c494fb0ed394ffb1af34c", Name:="Prior Attainment")>
    Public Function P125_PPAY1to4NOR As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim F100_AllAcademies As Decimal = F100_AllAcademies
        Dim FundingBasis As Decimal = F900_FUndingBasis
        Dim F200_SBS_Academies As Decimal = F200_SBS_Academies
        Dim NOR_P03_Y1Y4 As Decimal = NOR_P03_Y1Y4
        If F200_SBS_Academies = 1 then
            Result = NOR_P03_Y1Y4
        Else
            Exclude(rid)
        End iF

        Print(F100_AllAcademies, "F100_AllAcademies", rid)
        Print(FundingBasis, "FundingBasis", rid)
        Print(F200_SBS_Academies, "F200_SBS_Academies", rid)
        Print(NOR_P03_Y1Y4, "NOR_P03_Y1Y4", rid)
        Return result
    End Function

    <Calculation(Id:="88cdcd3af8dd4a9d87337c77d2c9ab7a")>
    <CalculationSpecification(Id:="P126_PPAPriNOR", Name:="P126_PPAPriNOR")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="c6b57f57424c494fb0ed394ffb1af34c", Name:="Prior Attainment")>
    Public Function P126_PPAPriNOR As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim F100_AllAcademies As Decimal = F100_AllAcademies
        Dim FundingBasis As decimal = F900_FundingBasis
        Dim F200_SBS_Academies As Decimal = F200_SBS_Academies
        Dim P22_Total_NOR_PRI_SBS As Decimal = NOR_P22_Total_NOR_PRI_SBS
        If F200_SBS_Academies = 1 then
            Result = P22_Total_NOR_PRI_SBS
        Else
            Exclude(rid)
        End iF

        Print(F100_AllAcademies, "F100_AllAcademies", rid)
        Print(FundingBasis, "FundingBasis", rid)
        Print(F200_SBS_Academies, "F200_SBS_Academies", rid)
        Print(P22_Total_NOR_PRI_SBS, "P22_Total_NOR_PRI_SBS", rid)
        Return result
    End Function

    <Calculation(Id:="7eed900275f648e791b001cd4fee6fcc")>
    <CalculationSpecification(Id:="P127_PPARate", Name:="P127_PPARate")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="c6b57f57424c494fb0ed394ffb1af34c", Name:="Prior Attainment")>
    Public Function P127_PPARate As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim F100_AllAcademies As Decimal = F100_AllAcademies
        Dim FundingBasis As Decimal = F900_FUndingBasis
        Dim F200_SBS_Academies As Decimal = F200_SBS_Academies
        Dim PA_PrimAmountPerPupil As Decimal = LaToProv(Datasets.APTProformadataset.PriorAttainmentPrimaryAmountPerPupil)
        If F200_SBS_Academies = 1 then
            Result = PA_PrimAmountPerPupil
        Else
            Exclude(rid)
        End iF

        Print(F100_AllAcademies, "F100_AllAcademies", rid)
        Print(FundingBasis, "FundingBasis", rid)
        Print(F200_SBS_Academies, "F200_SBS_Academies", rid)
        Print(PA_PrimAmountPerPupil, "PA_PrimAmountPerPupil", rid)
        Return result
    End Function

    <Calculation(Id:="52143580dc4e4114b8481927f13211b6")>
    <CalculationSpecification(Id:="P128_PPAWeighting", Name:="P128_PPAWeighting")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="c6b57f57424c494fb0ed394ffb1af34c", Name:="Prior Attainment")>
    Public Function P128_PPAWeighting As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim F100_AllAcademies As Decimal = F100_AllAcademies
        Dim FundingBasis As Decimal = F900_FundingBasis
        Dim F200_SBS_Academies As Decimal = F200_SBS_Academies
        Dim PAPriY1Weight As Decimal = LaToProv(Datasets.APTProformadataset.PriorAttainmentPrimarynewEFSPWeighting)
        If F200_SBS_Academies = 1 then
            Result = PAPriY1Weight
        Else
            Exclude(rid)
        End IF

        Print(F100_AllAcademies, "F100_AllAcademies", rid)
        Print(FundingBasis, "FundingBasis", rid)
        Print(F200_SBS_Academies, "F200_SBS_Academies", rid)
        Print(PAPriY1Weight, "PAPriY1Weight", rid)
        Return result
    End Function

    <Calculation(Id:="0a747d3cbe8749ae8a14bef223e106a7")>
    <CalculationSpecification(Id:="P129_PPAPupilsY5to6NotAchieving", Name:="P129_PPAPupilsY5to6NotAchieving")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="c6b57f57424c494fb0ed394ffb1af34c", Name:="Prior Attainment")>
    Public Function P129_PPAPupilsY5to6NotAchieving As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim F100_AllAcademies As Decimal = F100_AllAcademies
        Dim FundingBasis As Decimal = F900_FundingBasis
        Dim F200_SBS_Academies As Decimal = F200_SBS_Academies
        Dim P120_PPAindicator As string = P120_PPAindicator
        Dim P121_PPAY5to6Proportion73 As Decimal = P121_PPAY5to6Proportion73
        Dim P124_PPAY5to6NOR As Decimal = P124_PPAY5to6NOR
        Dim P122_PPAY5to6Proportion78 As Decimal = P122_PPAY5to6Proportion78
        If F200_SBS_Academies = 1 then
            IF P120_PPAindicator = "73" THEN
                Result = P121_PPAY5to6Proportion73 * P124_PPAY5to6NOR
            End IF

            IF P120_PPAindicator = "78" THEN
                Result = P122_PPAY5to6Proportion78 * P124_PPAY5to6NOR
            End IF
        ELSE
            Exclude(rid)
        End IF

        Print(F100_AllAcademies, "F100_AllAcademies", rid)
        Print(FundingBasis, "FundingBasis", rid)
        Print(F200_SBS_Academies, "F200_SBS_Academies", rid)
        Print(P120_PPAindicator, "P120_PPAindicator", rid)
        Print(P121_PPAY5to6Proportion73, "P121_PPAY5to6Proportion73", rid)
        Print(P124_PPAY5to6NOR, "P124_PPAY5to6NOR", rid)
        Print(P122_PPAY5to6Proportion78, "P122_PPAY5to6Proportion78", rid)
        Return result
    End Function

    <Calculation(Id:="e7f55407a8034ba5a887e9ad83e02c1c")>
    <CalculationSpecification(Id:="P130_PPAPupilsY1to4NotAchieving", Name:="P130_PPAPupilsY1to4NotAchieving")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="c6b57f57424c494fb0ed394ffb1af34c", Name:="Prior Attainment")>
    Public Function P130_PPAPupilsY1to4NotAchieving As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim F100_AllAcademies As Decimal = F100_AllAcademies
        Dim FundingBasis As Decimal = F900_FundingBasis
        Dim F200_SBS_Academies As Decimal = F200_SBS_Academies
        Dim P123_PPAY1to4ProportionUnder As Decimal = P123_PPAY1to4ProportionUnder
        Dim P125_PPAY1to4NOR As Decimal = P125_PPAY1to4NOR
        Dim P128_PPAWeighting As Decimal = P128_PPAWeighting
        If F200_SBS_Academies = 1 then
            Result = P123_PPAY1to4ProportionUnder * P128_PPAWeighting * P125_PPAY1to4NOR
        ELSE
            Exclude(rid)
        End IF

        Print(F100_AllAcademies, "F100_AllAcademies", rid)
        Print(FundingBasis, "FundingBasis", rid)
        Print(F200_SBS_Academies, "F200_SBS_Academies", rid)
        Print(P123_PPAY1to4ProportionUnder, "P123_PPAY1to4ProportionUnder", rid)
        Print(P125_PPAY1to4NOR, "P125_PPAY1to4NOR", rid)
        Print(P128_PPAWeighting, "P128_PPAWeighting", rid)
        Return result
    End Function

    <Calculation(Id:="aa45c4ec78764d2db54b52743fec03dd")>
    <CalculationSpecification(Id:="P131_PPATotalPupilsY1to6NotAchieving", Name:="P131_PPATotalPupilsY1to6NotAchieving")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="c6b57f57424c494fb0ed394ffb1af34c", Name:="Prior Attainment")>
    Public Function P131_PPATotalPupilsY1to6NotAchieving As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim F100_AllAcademies As Decimal = F100_AllAcademies
        Dim FundingBasis As Decimal = F900_FundingBasis
        Dim F200_SBS_Academies As Decimal = F200_SBS_Academies
        Dim P130_PPAPupilsY1to4NotAchieving As Decimal = P130_PPAPupilsY1to4NotAchieving
        Dim P129_PPAPupilsY5to6NotAchieving As Decimal = P129_PPAPupilsY5to6NotAchieving
        If F200_SBS_Academies = 1 then
            Result = P130_PPAPupilsY1to4NotAchieving + P129_PPAPupilsY5to6NotAchieving
        ELSE
            Exclude(rid)
        End IF

        Print(F100_AllAcademies, "F100_AllAcademies", rid)
        Print(FundingBasis, "FundingBasis", rid)
        Print(F200_SBS_Academies, "F200_SBS_Academies", rid)
        Print(P129_PPAPupilsY5to6NotAchieving, "P129_PPAPupilsY5to6NotAchieving", rid)
        Print(P130_PPAPupilsY1to4NotAchieving, "P130_PPAPupilsY1to4NotAchieving", rid)
        Return result
    End Function

    <Calculation(Id:="fc3a63d2586d4f549fc1b4368b16e251")>
    <CalculationSpecification(Id:="P132_PPATotalProportionNotAchieving", Name:="P132_PPATotalProportionNotAchieving")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="c6b57f57424c494fb0ed394ffb1af34c", Name:="Prior Attainment")>
    Public Function P132_PPATotalProportionNotAchieving As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim F100_AllAcademies As Decimal = F100_AllAcademies
        Dim FundingBasis As Decimal = F900_FundingBasis
        Dim F200_SBS_Academies As Decimal = F200_SBS_Academies
        Dim P131_PPATotalPupilsY1to6NotAchieving As Decimal = P131_PPATotalPupilsY1to6NotAchieving
        Dim P124_PPAY5to6NOR As Decimal = P124_PPAY5to6NOR
        Dim P125_PPAY1to4NOR As Decimal = P125_PPAY1to4NOR
        If F200_SBS_Academies = 1 then
            Result = P131_PPATotalPupilsY1to6NotAchieving / (P124_PPAY5to6NOR + P125_PPAY1to4NOR)
        ELSE
            Exclude(rid)
        End IF

        Print(F100_AllAcademies, "F100_AllAcademies", rid)
        Print(FundingBasis, "FundingBasis", rid)
        Print(F200_SBS_Academies, "F200_SBS_Academies", rid)
        Print(P131_PPATotalPupilsY1to6NotAchieving, "P131_PPATotalPupilsY1to6NotAchieving", rid)
        Print(P124_PPAY5to6NOR, "P124_PPAY5to6NOR", rid)
        Print(P125_PPAY1to4NOR, "P125_PPAY1to4NOR", rid)
        Return result
    End Function

    <Calculation(Id:="6b06729c01454890b45b3b977d78f126")>
    <CalculationSpecification(Id:="P133_PPATotalFunding", Name:="P133_PPATotalFunding")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="c6b57f57424c494fb0ed394ffb1af34c", Name:="Prior Attainment")>
    Public Function P133_PPATotalFunding As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim F100_AllAcademies As Decimal = F100_AllAcademies
        Dim FundingBasis As Decimal = F900_FundingBasis
        Dim F200_SBS_Academies As Decimal = F200_SBS_Academies
        Dim P126_PPAPriNOR As Decimal = P126_PPAPriNOR
        Dim P127_PPARate As Decimal = P127_PPARate
        Dim P132_PPATotalProportionNotAchieving As Decimal = P132_PPATotalProportionNotAchieving
        If F200_SBS_Academies = 1 then
            Result = P126_PPAPriNOR * P127_PPARate * P132_PPATotalProportionNotAchieving
        ELSE
            Exclude(rid)
        End IF

        Print(F100_AllAcademies, "F100_AllAcademies", rid)
        Print(FundingBasis, "FundingBasis", rid)
        Print(F200_SBS_Academies, "F200_SBS_Academies", rid)
        Print(P126_PPAPriNOR, "P126_PPAPriNOR", rid)
        Print(P127_PPARate, "P127_PPARate", rid)
        Print(P132_PPATotalProportionNotAchieving, "P132_PPATotalProportionNotAchieving", rid)
        Return result
    End Function

    <Calculation(Id:="a2ae1c9e06584e6bb5cccd314397ddc4")>
    <CalculationSpecification(Id:="P134_NSENPPA", Name:="P134_NSENPPA")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="c6b57f57424c494fb0ed394ffb1af34c", Name:="Prior Attainment")>
    Public Function P134_NSENPPA As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim F100_AllAcademies As Decimal = F100_AllAcademies
        Dim FundingBasis As Decimal = F900_FundingBasis
        Dim F200_SBS_Academies As Decimal = F200_SBS_Academies
        Dim P133_PPATotalFunding As Decimal = P133_PPATotalFunding
        Dim P134a_NSENPPA_Percent As Decimal = P134a_NSENPPA_Percent / 100
        If F200_SBS_Academies = 1 then
            Result = P133_PPATotalFunding * P134a_NSENPPA_Percent
        ELSE
            Exclude(rid)
        End IF

        Print(F100_AllAcademies, "F100_AllAcademies", rid)
        Print(FundingBasis, "FundingBasis", rid)
        Print(F200_SBS_Academies, "F200_SBS_Academies", rid)
        Print(P133_PPATotalFunding, "P133_PPATotalFunding", rid)
        Print(P134a_NSENPPA_Percent, "P134a_NSENPPA_Percent", rid)
        Return result
    End Function

    <Calculation(Id:="55907ab0a10344a88cad1fcbcd785621")>
    <CalculationSpecification(Id:="P134a_NSENPPA_Percent", Name:="P134a_NSENPPA_Percent")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="c6b57f57424c494fb0ed394ffb1af34c", Name:="Prior Attainment")>
    Public Function P134a_NSENPPA_Percent As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim F100_AllAcademies As Decimal = F100_AllAcademies
        Dim F200_SBS_Academies As Decimal = F200_SBS_Academies
        Dim PAPrimaryNotionalSEN As Decimal = LaToProv(Datasets.APTProformadataset.PriorAttainmentPrimaryNotionalSEN) * 100
        If F200_SBS_Academies = 1 then
            Result = PAPrimaryNotionalSEN
        ELSE
            Exclude(rid)
        End IF

        Print(F100_AllAcademies, "F100_AllAcademies", rid)
        Print(F200_SBS_Academies, "F200_SBS_Academies", rid)
        Print(PAPrimaryNotionalSEN, "PAPrimaryNotionalSEN", rid)
        Return result
    End Function

    <Calculation(Id:="96756073c0eb4a5db71fdf92deb6d72c")>
    <CalculationSpecification(Id:="P135_InYearPPASubtotal", Name:="P135_InYearPPASubtotal")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="c6b57f57424c494fb0ed394ffb1af34c", Name:="Prior Attainment")>
    Public Function P135_InYearPPASubtotal As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim F100_AllAcademies As Decimal = F100_AllAcademies
        Dim FundingBasis As Decimal = F900_FundingBasis
        Dim F200_SBS_Academies As Decimal = F200_SBS_Academies
        Dim P133_PPATotalFunding As Decimal = P133_PPATotalFunding
        Dim P001_1718DaysOpen As Decimal = P001_1718DaysOpen
        Dim Year_Days As Decimal = 365
        If F200_SBS_Academies = 1 then
            Result = P133_PPATotalFunding * P001_1718DaysOpen / Year_Days
        ELSE
            Exclude(rid)
        End IF

        Print(F100_AllAcademies, "F100_AllAcademies", rid)
        Print(FundingBasis, "FundingBasis", rid)
        Print(F200_SBS_Academies, "F200_SBS_Academies", rid)
        Print(P133_PPATotalFunding, "P133_PPATotalFunding", rid)
        Print(P001_1718DaysOpen, "P001_1718DaysOpen", rid)
        Print(Year_Days, "Year_Days", rid)
        Return result
    End Function

    <Calculation(Id:="71929431726b4b8faa1d0e478b31b56c")>
    <CalculationSpecification(Id:="P136_SecPA_Y7Factor", Name:="P136_SecPA_Y7Factor")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="c6b57f57424c494fb0ed394ffb1af34c", Name:="Prior Attainment")>
    Public Function P136_SecPA_Y7Factor As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="27782db6929945a9981c7b4be100b9d1")>
    <CalculationSpecification(Id:="P136a_SecPA_Y7NationalWeight", Name:="P136a_SecPA_Y7NationalWeight")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="c6b57f57424c494fb0ed394ffb1af34c", Name:="Prior Attainment")>
    Public Function P136a_SecPA_Y7NationalWeight As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim F200 As Decimal = F200_SBS_Academies
        Dim APTLowAttainmentY7 As Decimal = LAtoProv(Datasets.APTProformadataset.Secondarylowattainment(year7)weighting)
        If F200 = 1 then
            result = APTLowAttainmentY7
        else
            exclude(rid)
        End If

        Return result
    End Function

    <Calculation(Id:="7cd18451c1254fb5b8b44944792df421")>
    <CalculationSpecification(Id:="P138_SecPARate", Name:="P138_SecPARate")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="c6b57f57424c494fb0ed394ffb1af34c", Name:="Prior Attainment")>
    Public Function P138_SecPARate As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim F100_AllAcademies As Decimal = F100_AllAcademies
        Dim FundingBasis As Decimal = F900_FundingBasis
        Dim F200_SBS_Academies As Decimal = F200_SBS_Academies
        Dim PA_SecAmountPerPupil As Decimal = LaToProv(Datasets.APTProformadataset.PriorAttainmentSecondaryAmountPerPupil)
        If F200_SBS_Academies = 1 then
            Result = PA_SecAmountPerPupil
        Else
            Exclude(rid)
        End iF

        Print(F100_AllAcademies, "F100_AllAcademies", rid)
        Print(FundingBasis, "FundingBasis", rid)
        Print(F200_SBS_Academies, "F200_SBS_Academies", rid)
        Print(PA_SecAmountPerPupil, "PA_SecAmountPerPupil", rid)
        Return result
    End Function

    <Calculation(Id:="13e2b24bd66241b9a86538b8b41f43ea")>
    <CalculationSpecification(Id:="P138a_SecPA_AdjustedSecFactor", Name:="P138a_SecPA_AdjustedSecFactor")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="c6b57f57424c494fb0ed394ffb1af34c", Name:="Prior Attainment")>
    Public Function P138a_SecPA_AdjustedSecFactor As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim F200 As Decimal = F200_SBS_Academies
        Dim P08a_Y7 As Decimal = NOR_P08a_Y7
        Dim P08b_Y8to11 As Decimal = NOR_P08b_Y8to11
        Dim P136_SecPA_Y7Factor As Decimal = P136_SecPAFactor_Y7Factor
        Dim P136a_SecPA_Y7NationalWeight As Decimal = P136a_SecPA_Y7NationalWeight
        Dim P137_SecPA_Y8to11Factor As Decimal = P137_SecPA_Y8to11Factor
        If F200 = 1 then
            result =((P136_SecPA_Y7Factor * P08a_Y7 * P136a_SecPA_Y7NationalWeight) + (P137_SecPA_Y8to11Factor * P08b_Y8to11)) / (P08a_Y7 + P08b_Y8to11)
        Else
            exclude(rid)
        End if

        Return result
    End Function

    <Calculation(Id:="75ee41419d4f43bb92966b471507cb4e")>
    <CalculationSpecification(Id:="P139_SecPASubtotal", Name:="P139_SecPASubtotal")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="c6b57f57424c494fb0ed394ffb1af34c", Name:="Prior Attainment")>
    Public Function P139_SecPASubtotal As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim F100_AllAcademies As Decimal = F100_AllAcademies
        Dim FundingBasis As Decimal = F900_FundingBasis
        Dim F200_SBS_Academies As Decimal = F200_SBS_Academies
        Dim P138_SecPARate As Decimal = P138_SecPARate
        Dim P138a_SecPA_Y7Factor As Decimal = P138a_SecPA_AdjustedSecFactor
        Dim P25_Total_NOR_SEC_SBS As Decimal = NOR_P25_Total_NOR_SEC_SBS
        Dim calc As Decimal = P25_Total_NOR_SEC_SBS * P138_SecPARate * P138a_SecPA_Y7Factor
        Dim PA_Secondary As Decimal = Datasets.APTNewISBdataset.LowAttainmentS
        If F200_SBS_Academies <> 1 Then
            exclude(rid)
        End if

        If(F100_AllAcademies = 17181) Or (FundingBasis = 2 And F100_AllAcademies = 17182) Or (FundingBasis = 2 And F100_AllAcademies = 17183) then
            result = calc
        Else
            If(FundingBasis = 1 And F100_AllAcademies = 17182) Or (FundingBasis = 1 And F100_AllAcademies = 17183) then 'new opener
                Result = PA_Secondary
            End If
        End If

        Print(F100_AllAcademies, "F100_AllAcademies", rid)
        Print(FundingBasis, "FundingBasis", rid)
        Print(F200_SBS_Academies, "F200_SBS_Academies", rid)
        Print(P138_SecPARate, "P138_SecPARate", rid)
        Print(P25_Total_NOR_SEC_SBS, "P25_Total_NOR_SEC_SBS", rid)
        Print(PA_Secondary, "PA_Secondary", rid)
        Print(calc, "calc", rid)
        Return result
    End Function

    <Calculation(Id:="f2ebc74ef73e4848a9c11b9f2dc8d5ba")>
    <CalculationSpecification(Id:="P140_NSENSecPA", Name:="P140_NSENSecPA")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="c6b57f57424c494fb0ed394ffb1af34c", Name:="Prior Attainment")>
    Public Function P140_NSENSecPA As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim F100_AllAcademies As Decimal = F100_AllAcademies
        Dim FundingBasis As Decimal = F900_FundingBasis
        Dim F200_SBS_Academies As Decimal = F200_SBS_Academies
        Dim P139_SecPASubtotal As Decimal = P139_SecPASubtotal
        Dim P140a_NSENSecPA_Percent As Decimal = P140a_NSENSecPA_Percent / 100
        If F200_SBS_Academies = 1 then
            Result = P139_SecPASubtotal * P140a_NSENSecPA_Percent
        Else
            Exclude(rid)
        End iF

        Print(F100_AllAcademies, "F100_AllAcademies", rid)
        Print(FundingBasis, "FundingBasis", rid)
        Print(F200_SBS_Academies, "F200_SBS_Academies", rid)
        Print(P139_SecPASubtotal, "P139_SecPASubtotal", rid)
        Print(P140a_NSENSecPA_Percent, "P140a_NSENSecPA_Percent", rid)
        Return result
    End Function

    <Calculation(Id:="9253a4f3ac9f4909924f6ab15d1d858e")>
    <CalculationSpecification(Id:="P140a_NSENSecPA_Percent", Name:="P140a_NSENSecPA_Percent")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="c6b57f57424c494fb0ed394ffb1af34c", Name:="Prior Attainment")>
    Public Function P140a_NSENSecPA_Percent As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim F100_AllAcademies As Decimal = F100_AllAcademies
        Dim FundingBasis As Decimal = F900_FundingBasis
        Dim F200_SBS_Academies As Decimal = F200_SBS_Academies
        Dim PA_SecNotionalSEN As Decimal = LaToProv(Datasets.APTProformadataset.PriorAttainmentSecondaryNotionalSEN) * 100
        If F200_SBS_Academies = 1 then
            Result = PA_SecNotionalSEN
        Else
            Exclude(rid)
        End iF

        Print(F100_AllAcademies, "F100_AllAcademies", rid)
        Print(FundingBasis, "FundingBasis", rid)
        Print(F200_SBS_Academies, "F200_SBS_Academies", rid)
        Print(PA_SecNotionalSEN, "PA_SecNotionalSEN", rid)
        Return result
    End Function

    <Calculation(Id:="383246f6551d45369f006e3ad1edc499")>
    <CalculationSpecification(Id:="P141_InYearSecPASubtotal", Name:="P141_InYearSecPASubtotal")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="c6b57f57424c494fb0ed394ffb1af34c", Name:="Prior Attainment")>
    Public Function P141_InYearSecPASubtotal As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim F100_AllAcademies As Decimal = F100_AllAcademies
        Dim FundingBasis As Decimal = F900_FundingBasis
        Dim F200_SBS_Academies As Decimal = F200_SBS_Academies
        Dim P139_SecPASubtotal As Decimal = P139_SecPASubtotal
        Dim P001_1718DaysOpen As Decimal = P001_1718DaysOpen
        Dim Year_Days As Decimal = 365
        If F200_SBS_Academies = 1 then
            Result = P139_SecPASubtotal * P001_1718DaysOpen / Year_Days
        ELSE
            Exclude(rid)
        End IF

        Print(F100_AllAcademies, "F100_AllAcademies", rid)
        Print(FundingBasis, "FundingBasis", rid)
        Print(F200_SBS_Academies, "F200_SBS_Academies", rid)
        Print(P139_SecPASubtotal, "P139_SecPASubtotal", rid)
        Print(P001_1718DaysOpen, "P001_1617DaysOpen", rid)
        Print(Year_Days, "Year_Days", rid)
        Return result
    End Function

    <Calculation(Id:="615ca661108540d9b8f7f3974a0329a9")>
    <CalculationSpecification(Id:="P185a_Phase", Name:="P185a_Phase")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="4eda722dff5b4f6f9e3d13eae928be8a", Name:="Sparsity")>
    Public Function P185a_Phase As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim AcadFilter As Decimal = F200_SBS_Academies
        Dim APT_Inputs_and_Adjustments_Phase As String = Datasets.APTInputsandAdjustments.Phase
        Dim Census_Pupil_Characteristics_Phase As String = Datasets.CensusPupilCharacteristics.Phase
        Dim PhaseString As String = Datasets.APTInputsandAdjustments.Phase
        Print(APT_Inputs_and_Adjustments_Phase, "APT_Inputs_and_Adjustments_Phase", rid)
        Print(Census_Pupil_Characteristics_Phase, "Census_Pupil_Characteristics_Phase", rid)
        If AcadFilter = 1 Then
            If string.IsNullOrEmpty(PhaseString) Then
                If Census_Pupil_Characteristics_Phase = "Primary" Then
                    result = 1
                Else If Census_Pupil_Characteristics_Phase = "Middle-deemed Primary" Then
                    result = 2
                Else If Census_Pupil_Characteristics_Phase = "Secondary" Then
                    result = 3
                Else If Census_Pupil_Characteristics_Phase = "Middle-deemed Secondary" Then
                    result = 4
                Else If Census_Pupil_Characteristics_Phase = "All-through" Then
                    result = 5
                Else
                    result = 0
                End If
            Else
                If APT_Inputs_and_Adjustments_Phase = "Primary" Then
                    result = 1
                Else If APT_Inputs_and_Adjustments_Phase = "Middle-deemed Primary" Then
                    result = 2
                Else If APT_Inputs_and_Adjustments_Phase = "Secondary" Then
                    result = 3
                Else If APT_Inputs_and_Adjustments_Phase = "Middle-deemed Secondary" Then
                    result = 4
                Else If APT_Inputs_and_Adjustments_Phase = "All-through" Then
                    result = 5
                Else
                    result = 0
                End If
            End If
        Else
            exclude(rid)
        End If

        Return result
    End Function

    <Calculation(Id:="47f42a40914b4c0fa0141d20c08b348e")>
    <CalculationSpecification(Id:="P186_SparsityTaperFlagPri", Name:="P186_SparsityTaperFlagPri")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="4eda722dff5b4f6f9e3d13eae928be8a", Name:="Sparsity")>
    Public Function P186_SparsityTaperFlagPri As Decimal
        Dim result As Decimal
        Dim AcadFilter As Decimal = F200_SBS_Academies
        Dim SBS_SparsityTaperFlagPri As String = LAtoProv(Datasets.APTProformadataset.Fixedortaperedsparsityprimarylumpsum)
        If AcadFilter = 1 Then
            If SBS_SparsityTaperFlagPri = "Tapered" Then
                result = 1
            Else
                result = 0
            End if
        Else
            exclude(rid)
        End if

        Print(SBS_SparsityTaperFlagPri, "SBS_SparsityTaperFlagPri", rid)
        Return result
    End Function

    <Calculation(Id:="c0c02645afe44d1d97760e26b155a364")>
    <CalculationSpecification(Id:="P187_SparsityTaperFlagMid", Name:="P187_SparsityTaperFlagMid")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="4eda722dff5b4f6f9e3d13eae928be8a", Name:="Sparsity")>
    Public Function P187_SparsityTaperFlagMid As Decimal
        Dim result As Decimal
        Dim AcadFilter As Decimal = F200_SBS_Academies
        Dim SBS_SparsityTaperFlagMid As String = LAtoProv(Datasets.APTProformadataset.Fixedortaperedsparsitymiddleschoollumpsum)
        If AcadFilter = 1 Then
            If SBS_SparsityTaperFlagMid = "Tapered" Then
                result = 1
            Else
                result = 0
            End if
        Else
            exclude(rid)
        End if

        Print(SBS_SparsityTaperFlagMid, "SBS_SparsityTaperFlagMid", rid)
        Return result
    End Function

    <Calculation(Id:="3c3fd088667f465596b0be7504bf9334")>
    <CalculationSpecification(Id:="P188_SparsityTaperFlagSec", Name:="P188_SparsityTaperFlagSec")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="4eda722dff5b4f6f9e3d13eae928be8a", Name:="Sparsity")>
    Public Function P188_SparsityTaperFlagSec As Decimal
        Dim result As Decimal
        Dim AcadFilter As Decimal = F200_SBS_Academies
        Dim SBS_SparsityTaperFlagSec As String = LAtoProv(Datasets.APTProformadataset.Fixedortaperedsparsitysecondarylumpsum)
        If AcadFilter = 1 Then
            If SBS_SparsityTaperFlagSec = "Tapered" Then
                result = 1
            Else
                result = 0
            End if
        Else
            exclude(rid)
        End if

        Print(SBS_SparsityTaperFlagSec, "SBS_SparsityTaperFlagSec", rid)
        Return result
    End Function

    <Calculation(Id:="623e40f3c3e34ef8b5d86b031cf34553")>
    <CalculationSpecification(Id:="P189_SparsityTaperFlagAllThru", Name:="P189_SparsityTaperFlagAllThru")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="4eda722dff5b4f6f9e3d13eae928be8a", Name:="Sparsity")>
    Public Function P189_SparsityTaperFlagAllThru As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="f47c481a387e4b7596900a8c159d9bbb")>
    <CalculationSpecification(Id:="P190_SparsityUnit", Name:="P190_SparsityUnit")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="4eda722dff5b4f6f9e3d13eae928be8a", Name:="Sparsity")>
    Public Function P190_SparsityUnit As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim AcadFilter As Decimal = F200_SBS_Academies
        Dim Phase As Decimal = P185a_Phase
        Dim PriLumpSum As Decimal = LAtoProv(Datasets.APTProformadataset.SparsityPrimaryLumpSum)
        Dim SecLumpSum As Decimal = LAtoProv(Datasets.APTProformadataset.SparsitySecondaryLumpSum)
        Dim MidLumpSum As Decimal = LAtoProv(Datasets.APTProformadataset.SparsityMiddleSchoolLumpSum)
        Dim AllLumpSum As Decimal = LAtoProv(Datasets.APTProformadataset.SparsityAllThroughLumpSum)
        If AcadFilter = 1 Then
            If Phase = 1 Then
                result = PriLumpSum
            Else If Phase = 2 Or Phase = 4 Then
                result = MidLumpSum
            Else If Phase = 3 Then
                result = SecLumpSum
            Else If Phase = 5 Then
                result = AllLumpSum
            Else
                result = 0
            End if
        Else
            exclude(rid)
        End if

        Print(Phase, "Phase", rid)
        Print(PriLumpSum, "PriLumpSum", rid)
        Print(SecLumpSum, "SecLumpSum", rid)
        Print(MidLumpSum, "MidLumpSum", rid)
        Print(AllLumpSum, "AllLumpSum", rid)
        Return result
    End Function

    <Calculation(Id:="72b14cc1caea4fea853e359e1871a8c3")>
    <CalculationSpecification(Id:="P191_SparsityDistance", Name:="P191_SparsityDistance")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="4eda722dff5b4f6f9e3d13eae928be8a", Name:="Sparsity")>
    Public Function P191_SparsityDistance As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim AcadFilter As Decimal = F200_SBS_Academies
        Dim Phase As Decimal = P185a_Phase
        Dim PriDistanceCensus As Decimal = Datasets.CensusPupilCharacteristics.PrimarysparsityavDistanceto2ndschool
        Dim PriDistanceIandA As Decimal = Datasets.APTInputsandAdjustments.PrimarysparsityavDistanceto2ndschoolmiles
        Dim SecDistanceCensus As Decimal = Datasets.CensusPupilCharacteristics.SecondarysparsityavDistanceto2ndschool
        Dim SecDistanceIandA As Decimal = Datasets.APTInputsandAdjustments.SecondarysparsityavDistanceto2ndschoolmiles
        Dim PriDistAdjString as string = Datasets.APTInputsandAdjustments.PrimarysparsityavDistanceto2ndschoolmiles
        Dim SecDistAdjString As String = Datasets.APTInputsandAdjustments.SecondarysparsityavDistanceto2ndschoolmiles
        If AcadFilter = 1 Then
            If Phase = 1 Then
                If string.IsNullOrEmpty(PriDistAdjString) Then
                    result = PriDistanceCensus
                Else
                    result = PriDistanceIandA
                End if
            Else
                If string.IsNullOrEmpty(SecDistAdjString) Then
                    result = SecDistanceCensus
                Else
                    result = SecDistanceIandA
                End if
            End if
        Else
            exclude(rid)
        End if

        Print(Phase, "Phase", rid)
        Print(PriDistanceIandA, "PriDistanceIandA", rid)
        Print(PriDistanceCensus, "PriDistanceCensus", rid)
        Print(SecDistanceIandA, "SecDistanceIandA", rid)
        Print(SecDistanceCensus, "SecDistanceCensus", rid)
        Return result
    End Function

    <Calculation(Id:="b3c435bd39614c6a9dfb5f9a9af3b83b")>
    <CalculationSpecification(Id:="P192_SparsityDistThreshold", Name:="P192_SparsityDistThreshold")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="4eda722dff5b4f6f9e3d13eae928be8a", Name:="Sparsity")>
    Public Function P192_SparsityDistThreshold As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="df328142a8584a7c81095d91ce3e1500")>
    <CalculationSpecification(Id:="P193_SparsityDistMet_YN", Name:="P193_SparsityDistMet_YN")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="4eda722dff5b4f6f9e3d13eae928be8a", Name:="Sparsity")>
    Public Function P193_SparsityDistMet_YN As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="7db2e04733564c17a7b91fe65159ef9d")>
    <CalculationSpecification(Id:="P194_SparsityAveYGSize", Name:="P194_SparsityAveYGSize")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="4eda722dff5b4f6f9e3d13eae928be8a", Name:="Sparsity")>
    Public Function P194_SparsityAveYGSize As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim NOR26 As Decimal = NOR_P26_Total_NOR_SBS
        Dim AcadFilter As Decimal = F200_SBS_Academies
        Dim P212_PYG As Decimal = P212_PYG
        Dim P213_SYG As Decimal = P213_SYG
        If AcadFilter = 1 Then
            result = NOR26 / (P212_PYG + P213_SYG)
        Else
            exclude(rid)
        End if

        Print(NOR26, "NOR26", rid)
        Print(P212_PYG, "P212_PYG", rid)
        Print(P213_SYG, "P213_SYG", rid)
        Return result
    End Function

    <Calculation(Id:="4c0f2c31cf9e47f69b57a3ffd88b854e")>
    <CalculationSpecification(Id:="P195_SparsityYGThreshold", Name:="P195_SparsityYGThreshold")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="4eda722dff5b4f6f9e3d13eae928be8a", Name:="Sparsity")>
    Public Function P195_SparsityYGThreshold As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim AcadFilter As Decimal = F200_SBS_Academies
        Dim Phase As Decimal = P185a_Phase
        Dim PriAveYGThreshold As Decimal = LAtoProv(Datasets.APTProformadataset.Primarypupilnumberaverageyeargroupthreshold)
        Dim MidAveYGThreshold As Decimal = LAtoProv(Datasets.APTProformadataset.MiddleSchoolpupilnumberaverageyeargroupthreshold)
        Dim SecAveYGThreshold As Decimal = LAtoProv(Datasets.APTProformadataset.Secondarypupilnumberaverageyeargroupthreshold)
        Dim AllthruAveYGThreshold As Decimal = LAtoProv(Datasets.APTProformadataset.AllThroughpupilnumberaverageyeargroupthreshold)
        Dim PriAveYGThresholdString As String = LAtoProv(Datasets.APTProformadataset.Primarypupilnumberaverageyeargroupthreshold)
        Dim MidAveYGThresholdString As String = LAtoProv(Datasets.APTProformadataset.MiddleSchoolpupilnumberaverageyeargroupthreshold)
        Dim SecAveYGThresholdString As String = LAtoProv(Datasets.APTProformadataset.Secondarypupilnumberaverageyeargroupthreshold)
        Dim AllthruAveYGThresholdString As String = LAtoProv(Datasets.APTProformadataset.AllThroughpupilnumberaverageyeargroupthreshold)
        If AcadFilter = 1 Then
            If Phase = 1 Then
                If string.IsNullOrEmpty(PriAveYGThresholdString) Then
                    result = 21.4
                Else
                    result = PriAveYGThreshold
                End if
            Else If Phase = 2 Or Phase = 4 Then
                If string.IsNullOrEmpty(MidAveYGThresholdString) Then
                    result = 69.2
                Else
                    result = MidAveYGThreshold
                End if
            Else If Phase = 3 Then
                If string.IsNullOrEmpty(SecAveYGThresholdString) Then
                    result = 120
                Else
                    result = SecAveYGThreshold
                End if
            Else If Phase = 5 Then
                If string.IsNullOrEmpty(AllthruAveYGThresholdString) Then
                    result = 62.5
                Else
                    result = AllthruAveYGThreshold
                End if
            Else
                result = 0
            End if
        Else
            exclude(rid)
        End if

        Print(Phase, "Phase", rid)
        Print(PriAveYGThreshold, "PriAveYGThreshold", rid)
        Print(MidAveYGThreshold, "MidAveYGThreshold", rid)
        Print(SecAveYGThreshold, "SecAveYGThreshold", rid)
        Print(AllthruAveYGThreshold, "AllthruAveYGThreshold", rid)
        Return result
    End Function

    <Calculation(Id:="79728cc8f11546eca59c56f6140110f0")>
    <CalculationSpecification(Id:="P196_SparsityYGThresholdMet_YN", Name:="P196_SparsityYGThresholdMet_YN")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="4eda722dff5b4f6f9e3d13eae928be8a", Name:="Sparsity")>
    Public Function P196_SparsityYGThresholdMet_YN As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim AcadFilter As Decimal = F200_SBS_Academies
        Dim P194_SparsityAveYGSize As Decimal = P194_SparsityAveYGSize
        Dim P195_SparsityYGThreshold As Decimal = P195_SparsityYGThreshold
        If AcadFilter = 1 Then
            If P194_SparsityAveYGSize <= P195_SparsityYGThreshold And P194_SparsityAveYGSize > 0 Then
                result = 1
            Else
                result = 0
            End if
        Else
            exclude(rid)
        End if

        Print(P194_SparsityAveYGSize, "P194_SparsityAveYGSize", rid)
        Print(P195_SparsityYGThreshold, "P195_SparsityYGThreshold", rid)
        Return result
    End Function

    <Calculation(Id:="db2c8816cdbc4237b26a74d57b3db3c6")>
    <CalculationSpecification(Id:="P197_SparsityLumpSumSubtotal", Name:="P197_SparsityLumpSumSubtotal")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="4eda722dff5b4f6f9e3d13eae928be8a", Name:="Sparsity")>
    Public Function P197_SparsityLumpSumSubtotal As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="8e939fffab794c61938747623acaaf2f")>
    <CalculationSpecification(Id:="P198_SparsityTaperSubtotal", Name:="P198_SparsityTaperSubtotal")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="4eda722dff5b4f6f9e3d13eae928be8a", Name:="Sparsity")>
    Public Function P198_SparsityTaperSubtotal As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim Phase As Decimal = P185a_Phase
        Dim P186_SparsityTaperFlagPri As Decimal = P186_SparsityTaperFlagPri
        Dim P193_SparsityDistMet_YN As Decimal = P193_SparsityDistMet_YN
        Dim P196_SparsityYGThresholdMet_YN As Decimal = P196_SparsityYGThresholdMet_YN
        Dim P190_SparsityUnit As Decimal = P190_SparsityUnit
        Dim P187_SparsityTaperFlagMid As Decimal = P187_SparsityTaperFlagMid
        Dim P188_SparsityTaperFlagSec As Decimal = P188_SparsityTaperFlagSec
        Dim P189_SparsityTaperFlagAllthru As Decimal = P189_SparsityTaperFlagAllthru
        Dim APT_ISB_SparsityFunding As Decimal = Datasets.APTNewISBdataset.SparsityFunding
        Dim F100_AllAcademies As Decimal = F100_AllAcademies
        Dim FundingBasis As Decimal = F900_FundingBasis
        Dim P194_SparsityAveYGSize As Decimal = P194_SparsityAveYGSize
        Dim P195_SparsityYGThreshold As Decimal = P195_SparsityYGThreshold
        If F100_AllAcademies = 17181 Or (F100_AllAcademies = 17182 And FundingBasis = 2) Or (F100_AllAcademies = 17183 And FundingBasis = 2) Then
            If Phase = 1 And P186_SparsityTaperFlagPri = 1 And P193_SparsityDistMet_YN = 1 And P196_SparsityYGThresholdMet_YN = 1 then
                result = P190_SparsityUnit * (1 - (P194_SparsityAveYGSize / P195_SparsityYGThreshold))
            Else
                result = 0
                If Phase = 2 And P187_SparsityTaperFlagMid = 1 And P193_SparsityDistMet_YN = 1 And P196_SparsityYGThresholdMet_YN = 1 then
                    result = P190_SparsityUnit * (1 - (P194_SparsityAveYGSize / P195_SparsityYGThreshold))
                Else
                    result = 0
                    If Phase = 4 And P187_SparsityTaperFlagMid = 1 And P193_SparsityDistMet_YN = 1 And P196_SparsityYGThresholdMet_YN = 1 then
                        result = P190_SparsityUnit * (1 - (P194_SparsityAveYGSize / P195_SparsityYGThreshold))
                    Else
                        result = 0
                        If Phase = 3 And P188_SparsityTaperFlagSec = 1 And P193_SparsityDistMet_YN = 1 And P196_SparsityYGThresholdMet_YN = 1 then
                            result = P190_SparsityUnit * (1 - (P194_SparsityAveYGSize / P195_SparsityYGThreshold))
                        Else
                            result = 0
                            If Phase = 5 And P189_SparsityTaperFlagAllthru = 1 And P193_SparsityDistMet_YN = 1 And P196_SparsityYGThresholdMet_YN = 1 then
                                result = P190_SparsityUnit * (1 - (P194_SparsityAveYGSize / P195_SparsityYGThreshold))
                            Else
                                result = 0
                            End if
                        End if
                    End if
                End if
            End if
        ElseIf F100_AllAcademies <> 17181 And FundingBasis = 1 Then
            result = 0
        Else
            Exclude(rid)
        End If

        Print(Phase, "Phase", rid)
        Print(P190_SparsityUnit, "P190_SparsityUnit", rid)
        Print(P193_SparsityDistMet_YN, "P193_SparsityDistMet_YN", rid)
        Print(P196_SparsityYGThresholdMet_YN, "P196_SparsityYGThresholdMet_YN", rid)
        Print(P195_SparsityYGThreshold, "P195_SparsityYGThreshold", rid)
        Print(P194_SparsityAveYGSize, "P194_SparsityAveYGSize", rid)
        Print(P186_SparsityTaperFlagPri, "P186_SparsityTaperFlagPri", rid)
        Print(P187_SparsityTaperFlagMid, "P187_SparsityTaperFlagMid", rid)
        Print(P188_SparsityTaperFlagSec, "P188_SparsityTaperFlagSec", rid)
        Print(P189_SparsityTaperFlagAllthru, "P189_SparsityTaperFlagAllthru", rid)
        Return result
    End Function

    <Calculation(Id:="7cbba95815ce40ab85e61727042e0962")>
    <CalculationSpecification(Id:="P198a_SubtotalLump_Taper_For_FAP_Only", Name:="P198a_SubtotalLump_Taper_For_FAP_Only")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="4eda722dff5b4f6f9e3d13eae928be8a", Name:="Sparsity")>
    Public Function P198a_SubtotalLump_Taper_For_FAP_Only As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim AcadFilter As Decimal = F200_SBS_Academies
        Dim P197_SparsityLumpSumSubtotal As Decimal = P197_SparsityLumpSumSubtotal
        Dim P198_SparsityTaperSubtotal As Decimal = P198_SparsityTaperSubtotal
        If AcadFilter = 1 Then
            result = P197_SparsityLumpSumSubtotal + P198_SparsityTaperSubtotal
        Else
            exclude(rid)
        End If

        Print(P198_SparsityTaperSubtotal, "P198_SparsityTaperSubtotal", rid)
        Print(P197_SparsityLumpSumSubtotal, "P197_SparsityLumpSumSubtotal", rid)
        Return result
    End Function

    <Calculation(Id:="5aab131f17ec450a998b5a3c2970f0dd")>
    <CalculationSpecification(Id:="P199_InYearSparsityLumpSumSubtotal", Name:="P199_InYearSparsityLumpSumSubtotal")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="4eda722dff5b4f6f9e3d13eae928be8a", Name:="Sparsity")>
    Public Function P199_InYearSparsityLumpSumSubtotal As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="4607e635f7424839893c02bf25c54bd8")>
    <CalculationSpecification(Id:="P200_InYearSparsityTaperSubtotal", Name:="P200_InYearSparsityTaperSubtotal")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="4eda722dff5b4f6f9e3d13eae928be8a", Name:="Sparsity")>
    Public Function P200_InYearSparsityTaperSubtotal As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim P198_SparsityTaperSubtotal As Decimal = P198_SparsityTaperSubtotal
        Dim P001_1718DaysOpen As Decimal = P001_1718DaysOpen
        Dim Year_Days1718 As Decimal = P025_YearDays_1718
        Dim AcadFilter As Decimal = F200_SBS_Academies
        If AcadFilter = 1 Then
            result = P198_SparsityTaperSubtotal * (P001_1718DaysOpen / Year_Days1718)
        Else
            exclude(rid)
        End if

        Print(P198_SparsityTaperSubtotal, "P198_SparsityTaperSubtotal", rid)
        Print(P001_1718DaysOpen, "P001_1718DaysOpen", rid)
        Print(Year_Days1718, "Year_Days1718", rid)
        Return result
    End Function

    <Calculation(Id:="9fa4ee77b368405cb77537665505d7bf")>
    <CalculationSpecification(Id:="P200a_InYear_SubtotalLump_Taper_for_FAP_Only", Name:="P200a_InYear_SubtotalLump_Taper_for_FAP_Only")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="4eda722dff5b4f6f9e3d13eae928be8a", Name:="Sparsity")>
    Public Function P200a_InYear_SubtotalLump_Taper_for_FAP_Only As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="b374095dea004505a9f843795ae07bb5")>
    <CalculationSpecification(Id:="P212_PYG", Name:="P212_PYG")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="4eda722dff5b4f6f9e3d13eae928be8a", Name:="Sparsity")>
    Public Function P212_PYG As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim AcadFilter As Decimal = F200_SBS_Academies
        Dim NOR42a As Decimal = NOR_P42a_Year_Groups_Primary
        If AcadFilter = 1 Then
            result = NOR42a
        Else
            exclude(rid)
        End If

        Print(NOR42a, "NOR42a", rid)
        Return result
    End Function

    <Calculation(Id:="4bba3366d8d547c19b6e3dd58f3ffa48")>
    <CalculationSpecification(Id:="P213_SYG", Name:="P213_SYG")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="4eda722dff5b4f6f9e3d13eae928be8a", Name:="Sparsity")>
    Public Function P213_SYG As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim AcadFilter As Decimal = F200_SBS_Academies
        Dim NOR42b As Decimal = NOR_P42b_Year_Groups_Secondary
        If AcadFilter = 1 Then
            result = NOR42b
        Else
            exclude(rid)
        End If

        Print(NOR42b, "NOR42b", rid)
        Return result
    End Function

    <Calculation(Id:="24755b0c594e4327a5b5d39a2f7241fc")>
    <CalculationSpecification(Id:="P236_NSENSparsity", Name:="P236_NSENSparsity")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="4eda722dff5b4f6f9e3d13eae928be8a", Name:="Sparsity")>
    Public Function P236_NSENSparsity As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim AcadFilter As Decimal = F200_SBS_Academies
        Dim P197_SparsityLumpSumSubtotal As Decimal = P197_SparsityLumpSumSubtotal
        Dim P198_SparsityTaperSubtotal As Decimal = P198_SparsityTaperSubtotal
        Dim P236a_NSENSparsity_Percent As Decimal = P236a_NSENSparsity_Percent / 100
        If AcadFilter = 1 Then
            result =(P197_SparsityLumpSumSubtotal + P198_SparsityTaperSubtotal) * P236a_NSENSparsity_Percent
        Else
            exclude(rid)
        End If

        Print(P198_SparsityTaperSubtotal, "P198_SparsityTaperSubtotal", rid)
        Print(P197_SparsityLumpSumSubtotal, "P197_SparsityLumpSumSubtotal", rid)
        Print(P236a_NSENSparsity_Percent, "P236a_NSENSparsity_Percent", rid)
        Return result
    End Function

    <Calculation(Id:="9bf39d6a528b4d76b9a3098e4fe62945")>
    <CalculationSpecification(Id:="P236a_NSENSparsity_Percent", Name:="P236a_NSENSparsity_Percent")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="4eda722dff5b4f6f9e3d13eae928be8a", Name:="Sparsity")>
    Public Function P236a_NSENSparsity_Percent As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim Phase As Decimal = P185a_Phase
        Dim PriSparsityNotionalSEN As Decimal = LAtoProv(Datasets.APTProformadataset.PrimarySparsityNotionalSEN)
        Dim SecSparsityNotionalSEN As Decimal = LAtoProv(Datasets.APTProformadataset.SecondarySparsityNotionalSEN)
        Dim AcadFilter As Decimal = F200_SBS_Academies
        If AcadFilter = 1 Then
            If Phase = 1 Then
                result = PriSparsityNotionalSEN * 100
            Else If Phase = 2 Or Phase = 3 Or Phase = 4 Or Phase = 5 Then
                result = SecSparsityNotionalSEN * 100
            Else
                result = 0
            End if
        Else
            exclude(rid)
        End if

        Print(Phase, "Phase", rid)
        Print(PriSparsityNotionalSEN, "PriSparsityNotionalSEN", rid)
        Print(SecSparsityNotionalSEN, "SecSparsityNotionalSEN", rid)
        Return result
    End Function

    <Calculation(Id:="8e5875634cf542eaa7464e69c7a9c1b7")>
    <CalculationSpecification(Id:="P249_SplitSiteSubtotal", Name:="P249_SplitSiteSubtotal")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="6857aab8747c4d699b69bba5858b98a3", Name:="Split Sites")>
    Public Function P249_SplitSiteSubtotal As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="d994c2dcb48445bf946b4ef30acf328e")>
    <CalculationSpecification(Id:="P250_NSENSplitSites", Name:="P250_NSENSplitSites")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="6857aab8747c4d699b69bba5858b98a3", Name:="Split Sites")>
    Public Function P250_NSENSplitSites As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim AcadFilter As Decimal = F200_SBS_Academies
        Dim P249_SplitSitesSubtotal As Decimal = P249_SplitSitesSubtotal
        Dim P250a_NSENSplitSites_Percent As Decimal = P250a_NSENSplitSites_Percent / 100
        If AcadFilter = 1 then
            Result = P249_SplitSitesSubtotal * P250a_NSENSplitSites_Percent
        Else
            exclude(rid)
        End If

        Print(P249_SplitSitesSubtotal, "P249_SplitSitesSubtotal", rid)
        Print(P250a_NSENSplitSites_Percent, "P250a_NSENSplitSites_Percent", rid)
        Return result
    End Function

    <Calculation(Id:="f7cbb6fe06d340fb9fd585fafa820f8d")>
    <CalculationSpecification(Id:="P250a_NSENSplitSites_Percent", Name:="P250a_NSENSplitSites_Percent")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="6857aab8747c4d699b69bba5858b98a3", Name:="Split Sites")>
    Public Function P250a_NSENSplitSites_Percent As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim Notional_SEN As Decimal = LAtoProv(Datasets.APTProformadataset.SplitSitesNotionalSEN)
        Dim AcadFilter As Decimal = F200_SBS_Academies
        If AcadFilter = 1 then
            result = Notional_SEN * 100
        Else
            exclude(rid)
        End if

        Print(Notional_SEN, "Notional_SEN", rid)
        Return result
    End Function

    <Calculation(Id:="f7b3038b083c4d00b24e7ca22c215aec")>
    <CalculationSpecification(Id:="P251_InYearSplitSitesSubtotal", Name:="P251_InYearSplitSitesSubtotal")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="6857aab8747c4d699b69bba5858b98a3", Name:="Split Sites")>
    Public Function P251_InYearSplitSitesSubtotal As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim P249_SplitSitesSubtotal As Decimal = P249_SplitSitesSubtotal
        Dim P001_1718DaysOpen As Decimal = P001_1718DaysOpen
        Dim Year_Days1718 As Decimal = P025_YearDays_1718
        Dim AcadFilter As Decimal = F200_SBS_Academies
        If AcadFilter = 1 Then
            result = P249_SplitSitesSubtotal * (P001_1718DaysOpen / Year_Days1718)
        Else
            exclude(rid)
        End if

        Print(P249_SplitSitesSubtotal, "P249_SplitSitesSubtotal", rid)
        Print(P001_1718DaysOpen, "P001_1718DaysOpen", rid)
        Print(Year_Days1718, "Year_Days1718", rid)
        Return result
    End Function

    <Calculation(Id:="89c6f336dfd742f7aa8987a57bdc3de1")>
    <CalculationSpecification(Id:="P001_1718DaysOpen", Name:="P001_1718DaysOpen")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="b2e46eab20374cd1864365a94a7ab9b3", Name:="Totals")>
    Public Function P001_1718DaysOpen As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim F200_SBS_Academies As Decimal = F200_SBS_Academies
        Dim P001_1718DaysOpen As Decimal = P027_DaysOpen
        Print(P001_1718DaysOpen, "Days Open", rid)
        Print(F200_SBS_Academies, "F200_SBS_Academies", rid)
        If F200_SBS_Academies = 1 Then
            Result = P001_1718DaysOpen
        Else
            Exclude(rid)
        End If

        Return result
    End Function

    <Calculation(Id:="1325b353789f4c94beba1df554a47d22")>
    <CalculationSpecification(Id:="Lump_Sum_Total", Name:="Lump_Sum_Total")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="b2e46eab20374cd1864365a94a7ab9b3", Name:="Totals")>
    Public Function Lump_Sum_Total As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim acadfilter As Decimal = F200_SBS_Academies
        Dim P241 As Decimal = P241_priLumpSumSubtotal
        Dim P245 As Decimal = P245_SecLumpSumSubtotal
        If acadfilter = 1 then
            result = P241 + P245
        else
            exclude(rid)
        End If

        Return result
    End Function

    <Calculation(Id:="865ed926a3b54479bb3e1e87f6d9d913")>
    <CalculationSpecification(Id:="InYearLumpSum", Name:="InYearLumpSum")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="b2e46eab20374cd1864365a94a7ab9b3", Name:="Totals")>
    Public Function InYearLumpSum As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim acadfilter As Decimal = F200_SBS_Academies
        Dim P242 As Decimal = P242_InYearPriLumpSumSubtotal
        Dim P246 As Decimal = P246_InYearSecLumpSumSubtotal
        If acadfilter = 1 then
            result = P242 + P246
        else
            exclude(rid)
        End If

        Return result
    End Function

    <Calculation(Id:="0951759ae6784044ae5f101d678d0d8a")>
    <CalculationSpecification(Id:="P288_SBSFundingTotal", Name:="P288_SBSFundingTotal")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="b2e46eab20374cd1864365a94a7ab9b3", Name:="Totals")>
    Public Function P288_SBSFundingTotal As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim AcadFilter As Decimal = F200_SBS_Academies
        Dim P005 As Decimal = P005_PriBESubtotal
        Dim P010 As Decimal = P010_KS3_BESubtotal
        Dim P015 As Decimal = P015_KS4_BESubtotal
        Dim P022 As Decimal = P022_PriFSMSubtotal
        Dim P027 As Decimal = P027_PriFSM6Subtotal
        Dim P033 As Decimal = P033_SecFSMSubtotal
        Dim P038 As Decimal = P038_SecFSM6Subtotal
        Dim P044 As Decimal = P044_IDACIFPriSubtotal
        Dim P050 As Decimal = P050_IDACIEPriSubtotal
        Dim P056 As Decimal = P056_IDACIDPriSubtotal
        Dim P062 As Decimal = P062_IDACICPriSubtotal
        Dim P068 As Decimal = P068_IDACIBPriSubtotal
        Dim P074 As Decimal = P074_IDACIAPriSubtotal
        Dim P080 As Decimal = P080_IDACIFSecSubtotal
        Dim P086 As Decimal = P086_IDACIESecSubtotal
        Dim P092 As Decimal = P092_IDACIDSecSubtotal
        Dim P098 As Decimal = P098_IDACICSecSubtotal
        Dim P104 As Decimal = P104_IDACIBSecSubtotal
        Dim P110 As Decimal = P110_IDACIASecSubtotal
        Dim P117 As Decimal = P117_LACSubtotal
        Dim P133 As Decimal = P133_PPATotalFunding
        Dim P139 As Decimal = P139_SecPASubtotal
        Dim P145 As Decimal = P145_EAL1PriSubtotal
        Dim P150 As Decimal = P150_EAL2PriSubtotal
        Dim P155 As Decimal = P155_EAL3PriSubtotal
        Dim P161 As Decimal = P161_EAL1SecSubtotal
        Dim P166 As Decimal = P166_EAL2SecSubtotal
        Dim P171 As Decimal = P171_EAL3SecSubtotal
        Dim P177 As Decimal = P177_MobPriSubtotal
        Dim P183 As Decimal = P183_MobSecSubtotal
        Dim P197 As Decimal = P197_SparsityLumpSumSubtotal
        Dim P198 As Decimal = P198_SparsityTaperSubtotal
        Dim P241 As Decimal = P241_PriLumpSumSubtotal
        Dim P245 As Decimal = P245_SecLumpSumSubtotal
        Dim P249 As Decimal = P249_SplitSitesSubtotal
        Dim P252 As Decimal = P252_PFISubtotal
        Dim P255 As Decimal = P255_FringeSubtotal
        Dim P261 As Decimal = P261_Ex1Subtotal
        Dim P265 As Decimal = P265_Ex2Subtotal
        Dim P269 As Decimal = P269_Ex3Subtotal
        Dim P273 As Decimal = P273_Ex4Subtotal
        Dim P277 As Decimal = P277_Ex5Subtotal
        Dim P281 As Decimal = P281_Ex6Subtotal
        Dim P298 As Decimal = P298_Growth
        Dim P300 As Decimal = P300_SBSOutcomeAdjustment
        If AcadFilter = 1 Then
            Result = P005 + P010 + P015 + P022 + P027 + P033 + P038 + P044 + P050 + P056 + P062 + P068 + P074 + P080 + P086 + P092 + P098 + P104 + P110 + P117 + P133 + P139 + P145 + P150 + P155 + P161 + P166 + P171 + P177 + P183 + P197 + P198 + P241 + P245 + P249 + P252 + P255 + P261 + P265 + P269 + P273 + P277 + P281 + P298 + P300
        Else
            Exclude(rid)
        End If

        Return result
    End Function

    <Calculation(Id:="e5276896bfe444cd8bb451da9b6ed01c")>
    <CalculationSpecification(Id:="P289_InYearSBSFundingTotal", Name:="P289_InYearSBSFundingTotal")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="b2e46eab20374cd1864365a94a7ab9b3", Name:="Totals")>
    Public Function P289_InYearSBSFundingTotal As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim AcadFilter As Decimal = F200_SBS_Academies
        Dim P007 As Decimal = P007_InYearPriBE_Subtotal
        Dim P012 As Decimal = P012_InYearKS3_BESubtotal
        Dim P018 As Decimal = P018_InYearKS4_BESubtotal
        Dim P023 As Decimal = P023_InYearPriFSMSubtotal
        Dim P029 As Decimal = P029_InYearPriFSM6Subtotal
        Dim P034 As Decimal = P034_InYearSecFSMSubtotal
        Dim P040 As Decimal = P040_InYearSecFSM6Subtotal
        Dim P046 As Decimal = P046_InyearIDACIFPriSubtotal
        Dim P052 As Decimal = P052_InYearIDACIEPriSubtotal
        Dim P058 As Decimal = P058_InYearIDACIDPriSubtotal
        Dim P064 As Decimal = P064_InYearIDACICPriSubtotal
        Dim P070 As Decimal = P070_InYearIDACIBPriSubtotal
        Dim P076 As Decimal = P076_InYearIDACIAPriSubtotal
        Dim P082 As Decimal = P082_InYearIDACIFSecSubtotal
        Dim P088 As Decimal = P088_InYearIDACIESecSubtotal
        Dim P094 As Decimal = P094_InYearIDACIDSecSubtotal
        Dim P100 As Decimal = P100_InYearIDACICSecSubtotal
        Dim P106 As Decimal = P106_InYearIDACIBSecSubtotal
        Dim P112 As Decimal = P112_InYearIDACIASecSubtotal
        Dim P119 As Decimal = P119_InYearLACSubtotal
        Dim P135 As Decimal = P135_InYearPPASubtotal
        Dim P141 As Decimal = P141_InYearSecPASubtotal
        Dim P146 As Decimal = P146_InYearEAL1PriSubtotal
        Dim P151 As Decimal = P151_InYearEAL2PriSubtotal
        Dim P157 As Decimal = P157_InYearEAL3PriSubtotal
        Dim P162 As Decimal = P162_InYearEAL1SecSubtotal
        Dim P167 As Decimal = P167_InYearEAL2SecSubtotal
        Dim P173 As Decimal = P173_InYearEAL3SecSubtotal
        Dim P179 As Decimal = P179_InYearMobPriSubtotal
        Dim P185 As Decimal = P185_InYearMobSecSubtotal
        Dim P199 As Decimal = P199_InYearSparsityLumpSumSubtotal
        Dim P200 As Decimal = P200_InYearSparsityTaperSubtotal
        Dim P242 As Decimal = P242_InYearPriLumpSumSubtotal
        Dim P246 As Decimal = P246_InYearSecLumpSumSubtotal
        Dim P251 As Decimal = P251_InYearSplitSitesSubtotal
        Dim P254 As Decimal = P254_InYearPFISubtotal
        Dim P257 As Decimal = P257_InYearFringeSubtotal
        Dim P264 As Decimal = P264_InYearEx1Subtotal
        Dim P267 As Decimal = P267_InYearEx2Subtotal
        Dim P271 As Decimal = P271_InYearEx3Subtotal
        Dim P275 As Decimal = P275_InYearEx4Subtotal
        Dim P279 As Decimal = P279_InYearEx5Subtotal
        Dim P283 As Decimal = P283_InYearEx6Subtotal
        Dim P299 As Decimal = P299_InYearGrowth
        Dim P301 As Decimal = P301_InYearSBSOutcomeAdjustment
        print(P007, "P007", rid)
        print(P012, "P012", rid)
        print(P018, "P018", rid)
        print(P023, "P023", rid)
        print(P029, "P029", rid)
        print(P034, "P034", rid)
        print(P040, "P040", rid)
        print(P046, "P046", rid)
        print(P052, "P052", rid)
        print(P058, "P058", rid)
        print(P064, "P064", rid)
        print(P070, "P070", rid)
        print(P076, "P076", rid)
        print(P082, "P082", rid)
        print(P088, "P088", rid)
        print(P094, "P094", rid)
        print(P100, "P100", rid)
        print(P106, "P106", rid)
        print(P112, "P112", rid)
        print(P119, "P119", rid)
        print(P135, "P135", rid)
        print(P141, "P141", rid)
        print(P146, "P146", rid)
        print(P151, "P151", rid)
        print(P157, "P157", rid)
        print(P162, "P162", rid)
        print(P167, "P167", rid)
        print(P173, "P173", rid)
        print(P179, "P179", rid)
        print(P185, "P185", rid)
        print(P199, "P199", rid)
        print(P200, "P200", rid)
        print(P242, "P242", rid)
        print(P246, "P246", rid)
        print(P251, "P251", rid)
        print(P254, "P254", rid)
        print(P257, "P257", rid)
        print(P264, "P264", rid)
        print(P267, "P267", rid)
        print(P271, "P271", rid)
        print(P275, "P275", rid)
        print(P279, "P279", rid)
        print(P283, "P283", rid)
        print(P299, "P299", rid)
        print(P301, "P301", rid)
        If AcadFilter = 1 Then
            Result = P007 + P012 + P018 + P023 + P029 + P034 + P040 + P046 + P052 + P058 + P064 + P070 + P076 + P082 + P088 + P094 + P100 + P106 + P112 + P119 + P135 + P141 + P146 + P151 + P157 + P162 + P167 + P173 + P179 + P185 + P199 + P200 + P242 + P246 + P251 + P254 + P257 + P264 + P267 + P271 + P275 + P279 + P283 + P299 + P301
        Else
            Exclude(rid)
        End If

        Return result
    End Function

    <Calculation(Id:="0e70f394e1444ea988481d3604c9940f")>
    <CalculationSpecification(Id:="P290_ISBTotalSBSFunding", Name:="P290_ISBTotalSBSFunding")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="b2e46eab20374cd1864365a94a7ab9b3", Name:="Totals")>
    Public Function P290_ISBTotalSBSFunding As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim TotalAllocation As Decimal = Datasets.APTNewISBdataset.TotalAllocation
        Dim Rates As Decimal = Datasets.APTNewISBdataset.Rates
        Dim AcadFilter As Decimal = F200_SBS_Academies
        If AcadFilter = 1 Then
            result = TotalAllocation - Rates
        Else
            exclude(rid)
        End if

        Print(TotalAllocation, "TotalAllocation", rid)
        Print(Rates, "Rates", rid)
        Return result
    End Function

    <Calculation(Id:="d03e869defe24f8a9f88af36f75f565e")>
    <CalculationSpecification(Id:="P291_TotalPupilLedFactors", Name:="P291_TotalPupilLedFactors")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="b2e46eab20374cd1864365a94a7ab9b3", Name:="Totals")>
    Public Function P291_TotalPupilLedFactors As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim AcadFilter As Decimal = F200_SBS_Academies
        Dim P005 As Decimal = P005_PriBESubtotal
        Dim P010 As Decimal = P010_KS3_BESubtotal
        Dim P015 As Decimal = P015_KS4_BESubtotal
        Dim P022 As Decimal = P022_PriFSMSubtotal
        Dim P027 As Decimal = P027_PriFSM6Subtotal
        Dim P033 As Decimal = P033_SecFSMSubtotal
        Dim P038 As Decimal = P038_SecFSM6Subtotal
        Dim P044 As Decimal = P044_IDACIFPriSubtotal
        Dim P050 As Decimal = P050_IDACIEPriSubtotal
        Dim P056 As Decimal = P056_IDACIDPriSubtotal
        Dim P062 As Decimal = P062_IDACICPriSubtotal
        Dim P068 As Decimal = P068_IDACIBPriSubtotal
        Dim P074 As Decimal = P074_IDACIAPriSubtotal
        Dim P080 As Decimal = P080_IDACIFSecSubtotal
        Dim P086 As Decimal = P086_IDACIESecSubtotal
        Dim P092 As Decimal = P092_IDACIDSecSubtotal
        Dim P098 As Decimal = P098_IDACICSecSubtotal
        Dim P104 As Decimal = P104_IDACIBSecSubtotal
        Dim P110 As Decimal = P110_IDACIASecSubtotal
        Dim P117 As Decimal = P117_LACSuBtotal
        Dim P133 As Decimal = P133_PPATotalFunding
        Dim P139 As Decimal = P139_SecPASubtotal
        Dim P145 As Decimal = P145_EAL1PriSubtotal
        Dim P150 As Decimal = P150_EAL2PriSubtotal
        Dim P155 As Decimal = P155_EAL3PriSubtotal
        Dim P161 As Decimal = P161_EAL1SecSubtotal
        Dim P166 As Decimal = P166_EAL2SecSubtotal
        Dim P171 As Decimal = P171_EAL3SecSubtotal
        Dim P177 As Decimal = P177_MobPriSubtotal
        Dim P183 As Decimal = P183_MobSecSubtotal
        If AcadFilter = 1 Then
            Result = P005 + P010 + P015 + P022 + P027 + P033 + P038 + P044 + P050 + P056 + P062 + P068 + P074 + P080 + P086 + P092 + P098 + P104 + P110 + P117 + P133 + P139 + P145 + P150 + P155 + P161 + P166 + P171 + P177 + P183
        Else
            Exclude(rid)
        End If

        Return result
    End Function

    <Calculation(Id:="7fa3eb29e0594772b3f2245104ab2d1e")>
    <CalculationSpecification(Id:="P292_InYearTotalPupilLedfactors", Name:="P292_InYearTotalPupilLedfactors")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="b2e46eab20374cd1864365a94a7ab9b3", Name:="Totals")>
    Public Function P292_InYearTotalPupilLedfactors As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim AcadFilter As Decimal = F200_SBS_Academies
        Dim P007 As Decimal = P007_InYearPriBE_Subtotal
        Dim P012 As Decimal = P012_InYearKS3_BESubtotal
        Dim P018 As Decimal = P018_InYearKS4_BESubtotal
        Dim P023 As Decimal = P023_InYearPriFSMSubtotal
        Dim P029 As Decimal = P029_InYearPriFSM6Subtotal
        Dim P034 As Decimal = P034_InYearSecFSMSubtotal
        Dim P040 As Decimal = P040_InYearSecFSM6Subtotal
        Dim P046 As Decimal = P046_InyearIDACIFPriSubtotal
        Dim P052 As Decimal = P052_InYearIDACIEPriSubtotal
        Dim P058 As Decimal = P058_InYearIDACIDPriSubtotal
        Dim P064 As Decimal = P064_InYearIDACICPriSubtotal
        Dim P070 As Decimal = P070_InYearIDACIBPriSubtotal
        Dim P076 As Decimal = P076_InYearIDACIAPriSubtotal
        Dim P082 As Decimal = P082_InYearIDACIFSecSubtotal
        Dim P088 As Decimal = P088_InYearIDACIESecSubtotal
        Dim P094 As Decimal = P094_InYearIDACIDSecSubtotal
        Dim P100 As Decimal = P100_InYearIDACICSecSubtotal
        Dim P106 As Decimal = P106_InYearIDACIBSecSubtotal
        Dim P112 As Decimal = P112_InYearIDACIASecSubtotal
        Dim P119 As Decimal = P119_InYearLACSubtotal
        Dim P135 As Decimal = P135_InYearPPASubtotal
        Dim P141 As Decimal = P141_InYearSecPASubtotal
        Dim P146 As Decimal = P146_InYearEAL1PriSubtotal
        Dim P151 As Decimal = P151_InYearEAL2PriSubtotal
        Dim P157 As Decimal = P157_InYearEAL3PriSubtotal
        Dim P162 As Decimal = P162_InYearEAL1SecSubtotal
        Dim P167 As Decimal = P167_InYearEAL2SecSubtotal
        Dim P173 As Decimal = P173_InYearEAL3SecSubtotal
        Dim P179 As Decimal = P179_InYearMobPriSubtotal
        Dim P185 As Decimal = P185_InYearMobSecSubtotal
        If AcadFilter = 1 Then
            Result = P007 + P012 + P018 + P023 + P029 + P034 + P040 + P046 + P052 + P058 + P064 + P070 + P076 + P082 + P088 + P094 + P100 + P106 + P112 + P119 + P135 + P141 + P146 + P151 + P157 + P162 + P167 + P173 + P179 + P185
        Else
            Exclude(rid)
        End If

        Return result
    End Function

    <Calculation(Id:="037883d744bd4ffd87fcab8c9a2a5fc1")>
    <CalculationSpecification(Id:="P293_TotalOtherFactors", Name:="P293_TotalOtherFactors")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="b2e46eab20374cd1864365a94a7ab9b3", Name:="Totals")>
    Public Function P293_TotalOtherFactors As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim AcadFilter As Decimal = F200_SBS_Academies
        Dim P197 As Decimal = P197_SparsityLumpSumSubtotal
        Dim P198 As Decimal = P198_SparsityTaperSubtotal
        Dim P241 As Decimal = P241_PriLumpSumSubtotal
        Dim P245 As Decimal = P245_SecLumpSumSubtotal
        Dim P249 As Decimal = P249_SplitSitesSubtotal
        Dim P252 As Decimal = P252_PFISubtotal
        Dim P255 As Decimal = P255_FringeSubtotal
        Dim P261 As Decimal = P261_Ex1Subtotal
        Dim P265 As Decimal = P265_Ex2Subtotal
        Dim P269 As Decimal = P269_Ex3Subtotal
        Dim P273 As Decimal = P273_Ex4Subtotal
        Dim P277 As Decimal = P277_Ex5Subtotal
        Dim P281 As Decimal = P281_Ex6Subtotal
        Dim P298 As Decimal = P298_Growth
        Dim P300 As Decimal = P300_SBSOutcomeAdjustment
        If AcadFilter = 1 Then
            Result = P197 + P198 + P241 + P245 + P249 + P252 + P255 + P261 + P265 + P269 + P273 + P277 + P281 + P298 + P300
        Else
            Exclude(rid)
        End If

        Return result
    End Function

    <Calculation(Id:="7b17d38aeed740c790f1c5634fefbdf3")>
    <CalculationSpecification(Id:="P293a_TotalOtherFactors_NoExc", Name:="P293a_TotalOtherFactors_NoExc")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="b2e46eab20374cd1864365a94a7ab9b3", Name:="Totals")>
    Public Function P293a_TotalOtherFactors_NoExc As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim AcadFilter As Decimal = F200_SBS_Academies
        Dim P197 As Decimal = P197_SparsityLumpSumSubtotal
        Dim P198 As Decimal = P198_SparsityTaperSubtotal
        Dim P241 As Decimal = P241_PriLumpSumSubtotal
        Dim P245 As Decimal = P245_SecLumpSumSubtotal
        Dim P249 As Decimal = P249_SplitSitesSubtotal
        Dim P252 As Decimal = P252_PFISubtotal
        Dim P255 As Decimal = P255_FringeSubtotal
        If AcadFilter = 1 Then
            Result = P197 + P198 + P241 + P245 + P249 + P252 + P255
        Else
            Exclude(rid)
        End If

        Return result
    End Function

    <Calculation(Id:="e32e7f2b8a9144149830727f39b5dc9f")>
    <CalculationSpecification(Id:="P294_InYearTotalOtherFactors", Name:="P294_InYearTotalOtherFactors")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="b2e46eab20374cd1864365a94a7ab9b3", Name:="Totals")>
    Public Function P294_InYearTotalOtherFactors As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim AcadFilter As Decimal = F200_SBS_Academies
        Dim P199 As Decimal = P199_InYearSparsityLumpSumSubtotal
        Dim P200 As Decimal = P200_InYearSparsityTaperSubtotal
        Dim P242 As Decimal = P242_InYearPriLumpSumSubtotal
        Dim P246 As Decimal = P246_InYearSecLumpSumSubtotal
        Dim P251 As Decimal = P251_InYearSplitSitesSubtotal
        Dim P254 As Decimal = P254_InYearPFISubtotal
        Dim P257 As Decimal = P257_InYearFringeSubtotal
        Dim P264 As Decimal = P264_InYearEx1Subtotal
        Dim P267 As Decimal = P267_InYearEx2Subtotal
        Dim P271 As Decimal = P271_InYearEx3Subtotal
        Dim P275 As Decimal = P275_InYearEx4Subtotal
        Dim P279 As Decimal = P279_InYearEx5Subtotal
        Dim P283 As Decimal = P283_InYearEx6Subtotal
        Dim P299 As Decimal = P299_InYearGrowth
        Dim P301 As Decimal = P301_InYearSBSOutcomeAdjustment
        If AcadFilter = 1 Then
            Result = P199 + P200 + P242 + P246 + P251 + P254 + P257 + P264 + p267 + P271 + P275 + P279 + P283 + P299 + P301
        Else
            Exclude(rid)
        End If

        Return result
    End Function

    <Calculation(Id:="a4bf1bb312e74b9fa97633617c5f9cc6")>
    <CalculationSpecification(Id:="P294a_InYearTotalOtherFactors_NoExc", Name:="P294a_InYearTotalOtherFactors_NoExc")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="b2e46eab20374cd1864365a94a7ab9b3", Name:="Totals")>
    Public Function P294a_InYearTotalOtherFactors_NoExc As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="a014042ca3be432fa3d2b25f52c3d91d")>
    <CalculationSpecification(Id:="P295_Dedelegation", Name:="P295_Dedelegation")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="b2e46eab20374cd1864365a94a7ab9b3", Name:="Totals")>
    Public Function P295_Dedelegation As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="43aa56a8fd394c53a633ba6d9285dcf1")>
    <CalculationSpecification(Id:="P296_InYearDedelegation", Name:="P296_InYearDedelegation")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="b2e46eab20374cd1864365a94a7ab9b3", Name:="Totals")>
    Public Function P296_InYearDedelegation As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim F100_AllAcademies As Decimal = F100_AllAcademies
        Dim FundingBasis As Decimal = F900_FundingBasis
        Dim F200_SBS_Academies As Decimal = F200_SBS_Academies
        Dim P295_Dedelegation As Decimal = P295_Dedelegation
        Dim P001_1718DaysOpen As Decimal = P001_1718DaysOpen
        Dim Year_Days As Decimal = 365
        If F200_SBS_Academies = 1 then
            Result =(P295_Dedelegation) * P001_1718DaysOpen / Year_Days
        ELSE
            Exclude(rid)
        End If

        Print(F100_AllAcademies, "F100_AllAcademies", rid)
        Print(FundingBasis, "FundingBasis", rid)
        Print(F200_SBS_Academies, "F200_SBS_Academies", rid)
        Print(P295_Dedelegation, "P295_Dedelegation", rid)
        Print(P001_1718DaysOpen, "P001_1718DaysOpen", rid)
        Print(Year_Days, "Year_Days", rid)
        Return result
    End Function

    <Calculation(Id:="59508343ac954286baeef1cf07a0709c")>
    Public Function F100_AllAcademies As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim Date_Opened as date = Datasets.ProviderInformation.DateOpened
        Dim Budget_ID1718 as String = Datasets.FundingStreams.Budget(2017181)
        Dim ProviderType as string = Datasets.ProviderInformation.ProviderType
        Dim Funding_Basis1718 as String = Datasets.AcademyInformation.FundingBasis
        Dim SubType As String = Datasets.ProviderInformation.ProviderSubtype
        Dim PrevMain As Boolean = Datasets.AcademyInformation.AcademyPreviouslyMaintained
        Dim ConvertDate As Date = Datasets.ProviderInformation.ConvertDate
        Dim Budget_ID1617 as String = Datasets.FundingStreams.Budget(2016171)
        Dim Funding_Basis1617 as String = Datasets.Administration.Providers.Academy_Information.Academy_Parameters.Funding_Basis(2016171)
        print(Date_Opened, "DateOpen", rid)
        Print(Budget_ID1718, "Budget_ID1718", rid)
        Print(ProviderType, "ProvType", rid)
        Print(Funding_Basis1718, "Funding_Basis1718", rid)
        Print(SubType, "SubType", rid)
        Print(PrevMain, "PrevMain", rid)
        Print(ConvertDate, "ConvertDate", rid)
        Print(Budget_ID1617, "Budget_ID1617", rid)
        Print(Funding_Basis1617, "Funding_Basis1617", rid)
        If subtype = "14CTC" Then
            Exclude(RID)
        ELSE If currentscenario.periodid = 2017181 Then
            If String.IsNullOrEmpty(budget_ID1718) Or String.IsNullOrEmpty(Funding_Basis1718) Then
                Exclude(RID)
            Else If(Budget_ID1718.Contains("YPLRE") Or Budget_ID1718.Contains("ZCONV")) Or ((Budget_ID1718.Contains("YPLRC") Or Budget_ID1718.Contains("YPLRA") Or Budget_ID1718.Contains("YPLRD")) And PrevMain = True) Then
                If Date_Opened > "01 January 0001" And Date_Opened < "01 April 2017" Then
                    result = 17181
                Else If Date_Opened >= "01 April 2017" And Date_Opened < "01 September 2017" Then
                    result = 17182
                Else
                    result = 17183
                End If
            End If
        End If

        If currentscenario.periodid = 2016171 Then
            If String.IsNullOrEmpty(budget_ID1617) Or String.IsNullOrEmpty(Funding_Basis1617) Then
                Exclude(RID)
            Else If(Budget_ID1617.Contains("YPLRE") And Date_Opened >= "01 April 2017" And Date_Opened < "01 September 2017") Or (Budget_ID1617.Contains("ZCONV") And convertdate >= "01 April 2017" And convertdate < "01 September 2017") Or ((Budget_ID1617.Contains("YPLRC") Or Budget_ID1617.Contains("YPLRA") Or Budget_ID1617.Contains("YPLRD") And (Date_Opened >= "01 April 2017" And Date_Opened < "01 September 2017") Or (convertdate >= "01 April 2017" And convertdate < "01 September 2017")) And PrevMain = True) Then
                result = 16171
            Else
                Exclude(RID)
            End If
        End If

        Return result
    End Function

    <Calculation(Id:="0d90243918104ee7b5366ca45fd9d6a5")>
    Public Function F200_SBS_Academies As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim F100_AllAcademies As Decimal = F100_AllAcademies
        Dim FundingBasis As String = Datasets.AcademyInformation.FundingBasis
        Print(F100_AllAcademies, "F100_AllAcademies", rid)
        Print(FundingBasis, "Funding_Basis", rid)
        If(F100_AllAcademies = 17181 Or F100_AllAcademies = 17182 Or F100_AllAcademies = 17183) And (FundingBasis = "Census" Or FundingBasis = "Estimate") Then
            Result = 1
        Else
            Exclude(rid)
        End If

        Return result
    End Function

    <Calculation(Id:="fe8b5a3f0b164a568efc50f531485188")>
    Public Function F300_ESG_Academies_All As Decimal
        Dim result = Decimal.Zero
        Dim F100_AllAcademies As Decimal = F100_AllAcademies
        Dim FundingBasis As String = Datasets.AcademyInformation.FundingBasis
        Dim NOR_SBS As Decimal = NOR_P26_Total_NOR_SBS
        Dim PrevMaintained As String = Datasets.AcademyInformation.AcademyPreviouslyMaintained
        Print(F100_AllAcademies, F100_AllAcademies, rid)
        Print(FundingBasis, FundingBasis, rid)
        Print(NOR_SBS, NOR_SBS, rid)
        Print(PrevMaintained, PrevMaintained, rid)
        If string.IsNullOrEmpty(PrevMaintained) And NOR_SBS = 0 And (FundingBasis = "Census" Or FundingBasis = "Estimate") Then
            Result = 0
            If currentscenario.periodid = 2017181 And (F100_AllAcademies = 17181 Or F100_AllAcademies = 17182 Or F100_AllAcademies = 17183) Then
                Result = 1
            Else
                Exclude(rid)
            End If
        End If

        Return result
    End Function

    <Calculation(Id:="228390a2828a48d7b114d345f78bb90d")>
    Public Function F301_ESG_Academies_Mainstream As Decimal
        Dim result = Decimal.Zero
        Dim F300_ESG_Academies_All As Decimal = F300_ESG_Academies_All
        Dim FundingBasis As String = Datasets.AcademyInformation.FundingBasis
        Print(F300_ESG_Academies_All, F300_ESG_Academies_All, rid)
        Print(FundingBasis, FundingBasis, rid)
        If F300_ESG_Academies_All = 1 And (FundingBasis = "Census" Or FundingBasis = "Estimate") Then
            Result = 1
        Else
            Exclude(rid)
        End If

        Return result
    End Function

    <Calculation(Id:="73c3427f13434afc8577a8c908114b4e")>
    Public Function F302_ESG_Academies_PlacesLed As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim F300_ESG_AcademiesAll As Decimal = F300_ESG_Academies_All
        Dim Funding_Basis As String = Datasets.AcademyInformation.FundingBasis
        Print(F300_ESG_AcademiesAll, "F300_ESG_AcademiesAll", rid)
        Print(Funding_Basis, "Funding_Basis", rid)
        'ESG does Not exist In 1718 so Set To zero
        'If F300_ESG_AcademiesAll = 1 And (Funding_Basis = "Place") Then
        '    Result = 1
        'Else
        '    Exclude(rid)
        'End if
        result = 0
        Return result
    End Function

    <Calculation(Id:="143902f3edeb48e4ad141a309462a5e6")>
    Public Function F400_HN_Academies As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim F100_AllAcademies As Decimal = F100_AllAcademies
        If currentscenario.periodid = 2017181 And (F100_AllAcademies = 17181 Or F100_AllAcademies = 17182 Or F100_AllAcademies = 17183) Then
            Result = 1
        Else
            Exclude(rid)
        End if

        Return result
    End Function

    <Calculation(Id:="ebc816d6175d4e8ba4e68b2f15ad68c3")>
    Public Function F500_MFG_Academies As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim F100_AllAcademies As Decimal = F100_AllAcademies
        Dim Funding_Basis As String = Datasets.AcademyInformation.FundingBasis
        Print(F100_AllAcademies, "F100_AllAcademies", rid)
        Print(Funding_Basis, "Funding_Basis", rid)
        If(F100_AllAcademies = 17181 Or F100_AllAcademies = 17182 Or F100_AllAcademies = 17183) And (Funding_Basis = "Census" Or Funding_Basis = "Estimate") Then
            Result = 1
        Else
            Exclude(rid)
        End If

        Return result
    End Function

    <Calculation(Id:="d95500390dac424c97b3131e4fe13638")>
    Public Function F600_ESGProtection_Academies As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim F100_AllAcademies As Decimal = F100_AllAcademies
        Print(F100_AllAcademies, "F100_AllAcademies", rid)
        If(F100_AllAcademies = 17181 Or F100_AllAcademies = 17182 Or F100_AllAcademies = 17183) Then
            Result = 1
        Else
            Result = 0
        End If

        Return result
    End Function

    <Calculation(Id:="b3671ad280ad4014bc27cfceb0e6e56e")>
    Public Function F601_ESGProtection_Post16onlyAP As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim F600_ESGProtection_Academies As Decimal = F600_ESGProtection_Academies
        Dim Subtype As String = Datasets.ProviderInformation.ProviderSubtype
        Dim BudgetID As String = Datasets.FundingStreams.Budget(2017181)
        Print(F600_ESGProtection_Academies, "F600_ESGProtection_Academies", rid)
        Print(Subtype, "Subtype", rid)
        Print(BudgetID, "BudgetID", rid)
        If F600_ESGProtection_Academies = 1 Then
            If Subtype = "22AAP" And BudgetID.Contains("YPLRD") Then
                Result = 1
            Else
                Result = 0
            End if
        Else
            Exclude(rid)
        End if

        Return result
    End Function

    <Calculation(Id:="72af94f064b34d5fb1b925fc0b93fab8")>
    Public Function F602_ESGProtection_Mainstream As Decimal
        Dim result As Decimal = 0
        Dim ESGProtectionAll As Decimal = Products.AY1718_Acad_GlobalVariables.F600_ESGProtection_Academies
        Dim APPost16 As Decimal = Products.AY1718_Acad_GlobalVariables.F601_ESGProtection_APPost16
        Dim FundingBasis As String = DataSets.Administration.Providers.Academy_Information.Academy_Parameters.Funding_Basis(2017181)
        Dim PrevMaintained As String = Datasets.AcademyInformation.AcademyPreviouslyMaintained
        Dim NOR_SBS As Decimal = products.AY1718_Acad_NOR.P26_Total_NOR_SBS
        Print(ESGProtectionAll, "ESGProtectionAll", rid)
        Print(APPost16, "APPost16", rid)
        Print(FundingBasis, "FundingBasis", rid)
        If APPost16 = 1 Then
            Result = 1
        Else
            If String.IsNullOrEmpty(PrevMaintained) And NOR_SBS = 0 Then
                Result = 0
            Else
                If ESGProtectionAll = 1 Then
                    If APPost16 = 1 Or FundingBasis = "Estimate" Or FundingBasis = "Census" Then
                        Result = 1
                    Else
                        Result = 0
                    End If
                Else
                    Exclude(rid)
                End If
            End If
        End If

        Return result
    End Function

    <Calculation(Id:="69cb4d39512048f09e905edfaffcd42e")>
    Public Function F603_ESGProtection_PlaceLed As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim F600_ESGProtection_Academies As Decimal = F600_ESGProtection_Academies
        Dim Funding_Basis As String = Datasets.AcademyInformation.FundingBasis
        Print(F600_ESGProtection_Academies, "F600_ESGProtection_Academies", rid)
        Print(Funding_Basis, "Funding_Basis", rid)
        If F600_ESGProtection_Academies = 1 Then
            If Funding_Basis = "Place" Then
                Result = 1
            Else
                Result = 0
            End If
        Else
            Exclude(rid)
        End if

        Return result
    End Function

    <Calculation(Id:="65cd96c1780c4f37aff640e4e87a9db2")>
    Public Function F800_FSProtection_Academies As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim F100_AllAcademies As Decimal = F100_AllAcademies
        Dim Funding_Basis As string = Datasets.AcademyInformation.FundingBasis
        Dim SubType As String = Datasets.ProviderInformation.ProviderSubtype
        Print(F100_AllAcademies, "F100_AllAcademies", rid)
        Print(Funding_Basis, "Funding_Basis", rid)
        Print(SubType, "SubType", rid)
        If F100_AllAcademies = 17183 And (Funding_Basis = "Census" Or Funding_Basis = "Estimate") And (SubType = "12FSC" Or SubType = "13SSA" Or SubType = "15UTC") Then
            Result = 1
        Else
            Exclude(rid)
        End if

        Return result
    End Function

    <Calculation(Id:="a7744f8203e84dec819706b32098ef7a")>
    Public Function F900_FundingBasis As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim Date_Opened as date = Datasets.ProviderInformation.DateOpened
        Dim Budget_ID as String = Datasets.FundingStreams.Budget(2017181)
        Dim ProviderType as string = Datasets.ProviderInformation.ProviderType
        Dim Funding_Basis as String = Datasets.AcademyInformation.FundingBasis
        Print(Funding_Basis, "ABCD Type", rid)
        If Budget_ID isnot nothing then
            If currentscenario.periodid = 2017181 and Funding_Basis = "Census" and (Budget_ID.Contains("YPLRE") Or Budget_ID.Contains("ZCONV")) Then
                Result = 1
            ElseIf currentscenario.periodid = 2017181 and Funding_Basis = "Estimate" and (Budget_ID.Contains("YPLRE") Or Budget_ID.Contains("ZCONV")) Then
                Result = 2
            ElseIf currentscenario.periodid = 2017181 and (Funding_Basis = "Place") and (Budget_ID.Contains("YPLRE") Or Budget_ID.Contains("ZCONV")) Then
                Result = 3
            Else
                Exclude(rid)
            End If
        Else
            Exclude(rid)
        End If

        Return result
    End Function

    <Calculation(Id:="517b2836a2fd45198390151da0557c52")>
    Public Function Post16Filter As Decimal
        Dim result As Decimal = 0
        Dim AcademyFilter As Decimal = Products.AY1718_GlobalVariables.F100_AllAcademies
        Dim BudgetID As String = Datasets.FundingStreams.Budget(2017181)
        Dim PrevMaintained As Boolean = Datasets.AcademyInformation.AcademyNewOpener
        If String.IsNullOrEmpty(budgetID) Then
            Exclude(RID)
        Else
            If AcademyFilter = 17183 And BudgetID.Contains("YPLRE") then
                result = 2
            Else
                If AcademyFilter = 17183 And BudgetID.Contains("ZCONV") then
                    result = 1
                Else
                    If AcademyFilter = 17183 And ((BudgetID.Contains("YPLRC") Or BudgetID.Contains("YPLRA") Or BudgetID.Contains("YPLRD")) And PrevMaintained = True) then
                        result = 3
                    Else
                        result = 0
                    End If
                End If
            End If
        End If

        Return result
    End Function

    <Calculation(Id:="9929dadb3f0b454980558788e6611db3")>
    Public Function P001_ESG_MAIN_APP As Decimal
        Dim result = Decimal.Zero
        Dim F100_AllAcademies As Decimal = F100_AllAcademies
        Dim Funding_Basis As String = Datasets.AcademyInformation.FundingBasis
        Print(F100_AllAcademies, "F100_AllAcademies", rid)
        Print(Funding_Basis, "Funding_Basis", rid)
        If F100_AllAcademies > 0 Then
            Result = 0
        Else
            Exclude(rid)
        End If

        Return result
    End Function

    <Calculation(Id:="df9174c7a815442ba34de841a9b4134a")>
    Public Function P010_ESGP_Main_Thresh1 As Decimal
        Dim result = Decimal.Zero
        Dim F100_AllAcademies As Decimal = F100_AllAcademies
        Dim Funding_Basis As String = Datasets.AcademyInformation.FundingBasis
        Print(F100_AllAcademies, "F100_AllAcademies", rid)
        Print(Funding_Basis, "Funding_Basis", rid)
        If F100_AllAcademies > 0 Then
            Result = 77.00
        Else
            Exclude(rid)
        End If

        Return result
    End Function

    <Calculation(Id:="5c139ac48c9244daa8f125c8178e2b96")>
    Public Function P011_ESGP_Main_Thresh2 As Decimal
        Dim result = Decimal.Zero
        Dim F100_AllAcademies As Decimal = F100_AllAcademies
        Dim Funding_Basis As String = Datasets.AcademyInformation.FundingBasis
        Print(F100_AllAcademies, "F100_AllAcademies", rid)
        Print(Funding_Basis, "Funding_Basis", rid)
        If F100_AllAcademies > 0 Then
            Result = 87
        Else
            Exclude(rid)
        End If

        Return result
    End Function

    <Calculation(Id:="3e4d665805584606bc441ffcbd7ed870")>
    Public Function P012_ESGP_AP_Thresh1 As Decimal
        Dim result = Decimal.Zero
        Dim F100_AllAcademies As Decimal = F100_AllAcademies
        Dim Funding_Basis As String = Datasets.AcademyInformation.FundingBasis
        Print(F100_AllAcademies, "F100_AllAcademies", rid)
        Print(Funding_Basis, "Funding_Basis", rid)
        If F100_AllAcademies > 0 Then
            Result = 288.75
        Else
            Exclude(rid)
        End If

        Return result
    End Function

    <Calculation(Id:="e18efdb6dcac441ca31eed93b6d08c62")>
    Public Function P013_ESGP_AP_Thresh2 As Decimal
        Dim result = Decimal.Zero
        Dim F100_AllAcademies As Decimal = F100_AllAcademies
        Dim Funding_Basis As String = Datasets.AcademyInformation.FundingBasis
        Print(F100_AllAcademies, "F100_AllAcademies", rid)
        Print(Funding_Basis, "Funding_Basis", rid)
        If F100_AllAcademies > 0 Then
            Result = 326.25
        Else
            Exclude(rid)
        End If

        Return result
    End Function

    <Calculation(Id:="632593232f0a41c0bd23e45604bcee41")>
    Public Function P014_ESGP_Special_Thresh1 As Decimal
        Dim result = Decimal.Zero
        Dim F100_AllAcademies As Decimal = F100_AllAcademies
        Dim Funding_Basis As String = Datasets.AcademyInformation.FundingBasis
        Print(F100_AllAcademies, "F100_AllAcademies", rid)
        Print(Funding_Basis, "Funding_Basis", rid)
        If F100_AllAcademies > 0 Then
            Result = 327.25
        Else
            Exclude(rid)
        End If

        Return result
    End Function

    <Calculation(Id:="b599e4ca9b304e19ac62ee142fd60abc")>
    Public Function P015_ESGP_Special_Thresh2 As Decimal
        Dim result = Decimal.Zero
        Dim F100_AllAcademies As Decimal = F100_AllAcademies
        Dim Funding_Basis As String = Datasets.AcademyInformation.FundingBasis
        Print(F100_AllAcademies, "F100_AllAcademies", rid)
        Print(Funding_Basis, "Funding_Basis", rid)
        If F100_AllAcademies > 0 Then
            Result = 369.75
        Else
            Exclude(rid)
        End If

        Return result
    End Function

    <Calculation(Id:="e68e36e643b345d39305a38e15e5e731")>
    Public Function P016_MathsTopUp_APP As Decimal
        Dim result = Decimal.Zero
        Dim F100_AllAcademies As Decimal = F100_AllAcademies
        Dim Funding_Basis As String = Datasets.AcademyInformation.FundingBasis
        Print(F100_AllAcademies, "F100_AllAcademies", rid)
        Print(Funding_Basis, "Funding_Basis", rid)
        If F100_AllAcademies > 0 Then
            Result = 2500.00
        Else
            Exclude(rid)
        End If

        Return result
    End Function

    <Calculation(Id:="87610919b56948f39716a262a66d7009")>
    Public Function P017_MFG_Level As Decimal
        Dim result = Decimal.Zero
        Dim F100_AllAcademies As Decimal = F100_AllAcademies
        Dim Funding_Basis As String = Datasets.AcademyInformation.FundingBasis
        Print(F100_AllAcademies, "F100_AllAcademies", rid)
        Print(Funding_Basis, "Funding_Basis", rid)
        If F100_AllAcademies > 0 Then
            Result = -0.015
        Else
            Exclude(rid)
        End If

        Return result
    End Function

    <Calculation(Id:="aa338b8ee4a14d27a68a6b53d471d6fb")>
    Public Function P018_ESG_Main_APP_1617 As Decimal
        Dim result = Decimal.Zero
        Dim F100_AllAcademies As Decimal = F100_AllAcademies
        Dim Funding_Basis As String = Datasets.AcademyInformation.FundingBasis
        Print(F100_AllAcademies, "F100_AllAcademies", rid)
        Print(Funding_Basis, "Funding_Basis", rid)
        If F100_AllAcademies > 0 Then
            Result = 77.00
        Else
            Exclude(rid)
        End If

        Return result
    End Function

    <Calculation(Id:="ee8fe7355a6d47caae442be21344a98f")>
    Public Function P019_ESGP_Main_Rate_Change As Decimal
        Dim result = Decimal.Zero
        Dim F100_AllAcademies As Decimal = F100_AllAcademies
        Dim Funding_Basis As String = Datasets.AcademyInformation.FundingBasis
        Print(F100_AllAcademies, "F100_AllAcademies", rid)
        Print(Funding_Basis, "Funding_Basis", rid)
        If F100_AllAcademies > 0 Then
            Result = -77
        Else
            Exclude(rid)
        End If

        Return result
    End Function

    <Calculation(Id:="0bb26cd977b347a093f349ec58b7ab11")>
    Public Function P002_ESG_AP_APP As Decimal
        Dim result = Decimal.Zero
        Dim F100_AllAcademies As Decimal = F100_AllAcademies
        Dim Funding_Basis As String = Datasets.AcademyInformation.FundingBasis
        Print(F100_AllAcademies, "F100_AllAcademies", rid)
        Print(Funding_Basis, "Funding_Basis", rid)
        If F100_AllAcademies > 0 Then
            Result = 0
        Else
            Exclude(rid)
        End If

        Return result
    End Function

    <Calculation(Id:="0e885637042f4ea39f066d57ca81fb18")>
    Public Function P020_ESGP_Special_Rate_Change As Decimal
        Dim result = Decimal.Zero
        Dim F100_AllAcademies As Decimal = F100_AllAcademies
        Dim Funding_Basis As String = Datasets.AcademyInformation.FundingBasis
        Print(F100_AllAcademies, "F100_AllAcademies", rid)
        Print(Funding_Basis, "Funding_Basis", rid)
        If F100_AllAcademies > 0 Then
            Result = -327.25
        Else
            Exclude(rid)
        End If

        Return result
    End Function

    <Calculation(Id:="aa7173d37f8b4a3b828909f6511a1c1e")>
    Public Function P021_ESGP_AP_Rate_Change As Decimal
        Dim result = Decimal.Zero
        Dim F100_AllAcademies As Decimal = F100_AllAcademies
        Dim Funding_Basis As String = Datasets.AcademyInformation.FundingBasis
        Print(F100_AllAcademies, "F100_AllAcademies", rid)
        Print(Funding_Basis, "Funding_Basis", rid)
        If F100_AllAcademies > 0 Then
            Result = -288.75
        Else
            Exclude(rid)
        End If

        Return result
    End Function

    <Calculation(Id:="7c69375590274d0cb75df25300fc1828")>
    Public Function P022_RPA_Rate As Decimal
        Dim result = Decimal.Zero
        Dim F100_AllAcademies As Decimal = F100_AllAcademies
        Dim Funding_Basis As String = Datasets.AcademyInformation.FundingBasis
        Print(F100_AllAcademies, "F100_AllAcademies", rid)
        Print(Funding_Basis, "Funding_Basis", rid)
        If F100_AllAcademies > 0 Then
            Result = -20
        Else
            Exclude(rid)
        End If

        Return result
    End Function

    <Calculation(Id:="674a718c17ff487a94f7c5b4f40b8d9b")>
    Public Function P023_ESG_HN_APP_1617 As Decimal
        Dim result = Decimal.Zero
        Dim F100_AllAcademies As Decimal = F100_AllAcademies
        Dim Funding_Basis As String = Datasets.AcademyInformation.FundingBasis
        Print(F100_AllAcademies, "F100_AllAcademies", rid)
        Print(Funding_Basis, "Funding_Basis", rid)
        If F100_AllAcademies > 0 Then
            Result = 327.25
        Else
            Exclude(rid)
        End If

        Return result
    End Function

    <Calculation(Id:="dbd7e6d748b34ef4adbf495882ea40ca")>
    Public Function P024_ESG_AP_APP_1617 As Decimal
        Dim result = Decimal.Zero
        Dim F100_AllAcademies As Decimal = F100_AllAcademies
        Dim Funding_Basis As String = Datasets.AcademyInformation.FundingBasis
        Print(F100_AllAcademies, "F100_AllAcademies", rid)
        Print(Funding_Basis, "Funding_Basis", rid)
        If F100_AllAcademies > 0 Then
            Result = 288.75
        Else
            Exclude(rid)
        End If

        Return result
    End Function

    <Calculation(Id:="e88e128777f94921b3fe87f9c2cc0bb5")>
    Public Function P025_YearDays_1718 As Decimal
        Dim result = Decimal.Zero
        Dim F100_AllAcademies As Decimal = F100_AllAcademies
        Dim Funding_Basis As String = Datasets.AcademyInformation.FundingBasis
        Print(F100_AllAcademies, "F100_AllAcademies", rid)
        Print(Funding_Basis, "Funding_Basis", rid)
        If F100_AllAcademies > 0 Then
            Result = 365
        Else
            Exclude(rid)
        End If

        Return result
    End Function

    <Calculation(Id:="696b85d2ed5741cf8b8bd8da05a80478")>
    Public Function P026_YearDays_1617 As Decimal
        Dim result = Decimal.Zero
        Dim F100_AllAcademies As Decimal = F100_AllAcademies
        Dim Funding_Basis As String = Datasets.AcademyInformation.FundingBasis
        Print(F100_AllAcademies, "F100_AllAcademies", rid)
        Print(Funding_Basis, "Funding_Basis", rid)
        If F100_AllAcademies > 0 Then
            Result = 365
        Else
            Exclude(rid)
        End If

        Return result
    End Function

    <Calculation(Id:="9ee0c1519dfd44228e2ef25ef9f86603")>
    Public Function P027_DaysOpen As Decimal
        Dim result = Decimal.Zero
        Dim F100_AllAcademies As Decimal = F100_AllAcademies
        Dim Funding_Basis As String = Datasets.AcademyInformation.FundingBasis
        Dim DateOpened As Date = Datasets.ProviderInformation.DateOpened
        Dim ConvertDate As Date = Datasets.ProviderInformation.ConvertDate
        Dim DateUsed As Date
        Dim Days_Open As Decimal = 0
        If ConvertDate >= DateOpened Then
            DateUsed = ConvertDate
        Else
            DateUsed = DateOpened
        End If

        Print(DateOpened, "Date Opened", rid)
        Print(ConvertDate, "Convert Date", rid)
        Print(F100_AllAcademies, "F100_AllAcademies", rid)
        Print(Funding_Basis, "Funding_Basis", rid)
        If F100_AllAcademies > 0 then
            Days_Open = DateDiff("d", DateUsed, "1 September 2018")
            Print(Days_Open, "Number of Days Open to end 1718", rid)
            Print(F100_AllAcademies, "F100_AllAcademies", rid)
            if Days_Open > 365 then
                result = 365
            else
                result = Days_Open
            End if
        else
            exclude(rid)
        End If

        Return result
    End Function

    <Calculation(Id:="54977ba7980d4cefa71e37aded9a2cb9")>
    Public Function P028_MonthsOpen As Decimal
        Dim result As Decimal = 0
        Dim F100_AllAcademies As Decimal = F100_AllAcademies
        Dim DateOpened As Date = Datasets.ProviderInformation.DateOpened
        Dim ConvertDate As Date = Datasets.ProviderInformation.ConvertDate
        Dim DateUsed As Date
        Dim Months_Open As Decimal = 0
        If ConvertDate >= DateOpened Then
            DateUsed = ConvertDate
        Else
            DateUsed = DateOpened
        End If

        Months_Open = DateDiff("m", DateUsed, "1 September 2017")
        If F100_AllAcademies > 0 then
            If Months_Open < 12 then
                Result = Months_Open 'does this need To be /12 ie For an academy oepning October this would be 11/12ths?
            Else
                Result = 12
            End If
        Else
            Exclude(rid)
        End If

        Print(F100_AllAcademies, "F100_AllAcademies", rid)
        Print(Months_Open, "Number of Months Open", rid)
        Print(ConvertDate, "Convert Date", rid)
        Print(DateOpened, "Date Opened", rid)
        Return result
    End Function

    <Calculation(Id:="422cb930ef01499ea103f1eccbcb0045")>
    Public Function P029_FSP_Level As Decimal
        Dim result = Decimal.Zero
        Dim F100_AllAcademies As Decimal = F100_AllAcademies
        Dim Funding_Basis As String = Datasets.AcademyInformation.FundingBasis
        Print(F100_AllAcademies, "F100_AllAcademies", rid)
        Print(Funding_Basis, "Funding_Basis", rid)
        If F100_AllAcademies > 0 Then
            Result = -0.015
        Else
            Exclude(rid)
        End If

        Return result
    End Function

    <Calculation(Id:="d0986776c4964300a2801a8f73baa0e4")>
    Public Function P003_ESG_HN_APP As Decimal
        Dim result = Decimal.Zero
        Dim F100_AllAcademies As Decimal = F100_AllAcademies
        Dim Funding_Basis As String = Datasets.AcademyInformation.FundingBasis
        Print(F100_AllAcademies, "F100_AllAcademies", rid)
        Print(Funding_Basis, "Funding_Basis", rid)
        If F100_AllAcademies > 0 Then
            Result = 0
        Else
            Exclude(rid)
        End If

        Return result
    End Function

    <Calculation(Id:="b6e67fb486874edcb1b42b9a0d57a657")>
    Public Function P004_Pre16_HN_APP As Decimal
        Dim result = Decimal.Zero
        Dim F100_AllAcademies As Decimal = F100_AllAcademies
        Dim Funding_Basis As String = Datasets.AcademyInformation.FundingBasis
        Print(F100_AllAcademies, "F100_AllAcademies", rid)
        Print(Funding_Basis, "Funding_Basis", rid)
        If F100_AllAcademies > 0 Then
            Result = 10000.00
        Else
            Exclude(rid)
        End If

        Return result
    End Function

    <Calculation(Id:="2503e48bb2e74d56877ffdb3f48c5142")>
    Public Function P005_Pre16_AP_APP As Decimal
        Dim result = Decimal.Zero
        Dim F100_AllAcademies As Decimal = F100_AllAcademies
        Dim Funding_Basis As String = Datasets.AcademyInformation.FundingBasis
        Print(F100_AllAcademies, "F100_AllAcademies", rid)
        Print(Funding_Basis, "Funding_Basis", rid)
        If F100_AllAcademies > 0 Then
            Result = 10000.00
        Else
            Exclude(rid)
        End If

        Return result
    End Function

    <Calculation(Id:="d5c33c1eec0947358600c0ec01ba195b")>
    Public Function P006_Post16_HN_APP As Decimal
        Dim result = Decimal.Zero
        Dim F100_AllAcademies As Decimal = F100_AllAcademies
        Dim Funding_Basis As String = Datasets.AcademyInformation.FundingBasis
        Print(F100_AllAcademies, "F100_AllAcademies", rid)
        Print(Funding_Basis, "Funding_Basis", rid)
        If F100_AllAcademies > 0 Then
            Result = 6000.00
        Else
            Exclude(rid)
        End If

        Return result
    End Function

    <Calculation(Id:="82ae56e9d5ab457bbda82d6a5b77b471")>
    Public Function P007_ESGP_Cond1 As Decimal
        Dim result = Decimal.Zero
        Dim F100_AllAcademies As Decimal = F100_AllAcademies
        Dim Funding_Basis As String = Datasets.AcademyInformation.FundingBasis
        Print(F100_AllAcademies, "F100_AllAcademies", rid)
        Print(Funding_Basis, "Funding_Basis", rid)
        If F100_AllAcademies > 0 Then
            Result = 0.01
        Else
            Exclude(rid)
        End If

        Return result
    End Function

    <Calculation(Id:="861195b5aefb40faae4458be453e3339")>
    Public Function P008_ESGP_Cond2 As Decimal
        Dim result = Decimal.Zero
        Dim F100_AllAcademies As Decimal = F100_AllAcademies
        Dim Funding_Basis As String = Datasets.AcademyInformation.FundingBasis
        Print(F100_AllAcademies, "F100_AllAcademies", rid)
        Print(Funding_Basis, "Funding_Basis", rid)
        If F100_AllAcademies > 0 Then
            Result = 0.02
        Else
            Exclude(rid)
        End If

        Return result
    End Function

    <Calculation(Id:="bcf02330f1be45e3b56b949cbe3b0cd1")>
    Public Function P009_ESGP_Cond3 As Decimal
        Dim result = Decimal.Zero
        Dim F100_AllAcademies As Decimal = F100_AllAcademies
        Dim Funding_Basis As String = Datasets.AcademyInformation.FundingBasis
        Print(F100_AllAcademies, "F100_AllAcademies", rid)
        Print(Funding_Basis, "Funding_Basis", rid)
        If F100_AllAcademies > 0 Then
            Result = 0.03
        Else
            Exclude(rid)
        End If

        Return result
    End Function

    <Calculation(Id:="d118b0d80d6749a78bd6cccc9a840d53")>
    Public Function POG_PPR_P03_FundingRate As Decimal
        Dim result = Decimal.Zero
        Dim AcademyFilter As Decimal = F100_AllAcademies
        Print(AcademyFilter, "Filter", rid)
        If AcademyFilter > 0 Then
            Result = 250
        Else
            Exclude(rid)
        End If

        Return result
    End Function

    <Calculation(Id:="e1bedcefc4cd4567ad71a95e8313dbac")>
    Public Function POG_PPR_P05_Sec_FundingRate As Decimal
        Dim result = Decimal.Zero
        Dim AcademyFilter As Decimal = F100_AllAcademies
        Print(AcademyFilter, "Filter", rid)
        If AcademyFilter > 0 Then
            Result = 500
        Else
            Exclude(rid)
        End If

        Return result
    End Function

    <Calculation(Id:="3a14898e914745d5bb8c55616de26df3")>
    Public Function POG_PPR_P08_PriPlace_FundingRate As Decimal
        Dim result = Decimal.Zero
        Dim AcademyFilter As Decimal = F100_AllAcademies
        Print(AcademyFilter, "Filter", rid)
        If AcademyFilter > 0 Then
            Result = 250
        Else
            Exclude(rid)
        End If

        Return result
    End Function

    <Calculation(Id:="afe86ae8ba6b48bd90943dd28c36906a")>
    Public Function POG_PPR_P10_SecPlace_FundingRate As Decimal
        Dim result = Decimal.Zero
        Dim AcademyFilter As Decimal = F100_AllAcademies
        Print(AcademyFilter, "Filter", rid)
        If AcademyFilter > 0 Then
            Result = 500
        Else
            Exclude(rid)
        End If

        Return result
    End Function

    <Calculation(Id:="ccb760e1f2e9476e91c278744c465295")>
    Public Function POG_LD_P19_SecEmptyCohort1 As Decimal
        Dim result = Decimal.Zero
        Dim AcademyFilter As Decimal = F100_AllAcademies
        Print(AcademyFilter, "Filter", rid)
        If AcademyFilter > 0 Then
            Result = 31000
        Else
            Exclude(rid)
        End If

        Return result
    End Function

    <Calculation(Id:="85b1f8322263414b88633d106459e1ca")>
    Public Function POG_LD_P13_PriEmptyCohort2 As Decimal
        Dim result = Decimal.Zero
        Dim AcademyFilter As Decimal = F100_AllAcademies
        Print(AcademyFilter, "Filter", rid)
        If AcademyFilter > 0 Then
            Result = 27000
        Else
            Exclude(rid)
        End If

        Return result
    End Function

    <Calculation(Id:="166512959f294e3cab1489ade0629376")>
    Public Function POG_LD_P14_PriEmptyCohort3 As Decimal
        Dim result = Decimal.Zero
        Dim AcademyFilter As Decimal = F100_AllAcademies
        Print(AcademyFilter, "Filter", rid)
        If AcademyFilter > 0 Then
            Result = 40500
        Else
            Exclude(rid)
        End If

        Return result
    End Function

    <Calculation(Id:="d4d3c87781774a70ab5fc57869962954")>
    Public Function POG_LD_P15_PriEmptyCohort4 As Decimal
        Dim result = Decimal.Zero
        Dim AcademyFilter As Decimal = F100_AllAcademies
        Print(AcademyFilter, "Filter", rid)
        If AcademyFilter > 0 Then
            Result = 54000
        Else
            Exclude(rid)
        End If

        Return result
    End Function

    <Calculation(Id:="39525b718ad54793960f36213cd472ce")>
    Public Function POG_LD_P16_PriEmptyCohort5 As Decimal
        Dim result = Decimal.Zero
        Dim AcademyFilter As Decimal = F100_AllAcademies
        Print(AcademyFilter, "Filter", rid)
        If AcademyFilter > 0 Then
            Result = 67500
        Else
            Exclude(rid)
        End If

        Return result
    End Function

    <Calculation(Id:="4367dcb708ab44a697c9bb84d35d7053")>
    Public Function POG_LD_P17_PriEmptyCohort6 As Decimal
        Dim result = Decimal.Zero
        Dim AcademyFilter As Decimal = F100_AllAcademies
        Print(AcademyFilter, "Filter", rid)
        If AcademyFilter > 0 Then
            Result = 80500
        Else
            Exclude(rid)
        End If

        Return result
    End Function

    <Calculation(Id:="375c79b75e9c45ab8d5c32abaeb8420c")>
    Public Function POG_LD_P18_Pri_Max_Cap As Decimal
        Dim result = Decimal.Zero
        Dim AcademyFilter As Decimal = F100_AllAcademies
        Print(AcademyFilter, "Filter", rid)
        If AcademyFilter > 0 Then
            Result = 283000
        Else
            Exclude(rid)
        End If

        Return result
    End Function

    <Calculation(Id:="615b0b17a340437c8e8f1a3ff051bef5")>
    Public Function POG_LD_P12_PriEmptyCohort1 As Decimal
        Dim result = Decimal.Zero
        Dim AcademyFilter As Decimal = F100_AllAcademies
        Print(AcademyFilter, "Filter", rid)
        If AcademyFilter > 0 Then
            Result = 13500
        Else
            Exclude(rid)
        End If

        Return result
    End Function

    <Calculation(Id:="3d7cf0e3050b41029069c4298d25ef01")>
    Public Function POG_LD_P20_SecEmptyCohort2 As Decimal
        Dim result = Decimal.Zero
        Dim AcademyFilter As Decimal = F100_AllAcademies
        Print(AcademyFilter, "Filter", rid)
        If AcademyFilter > 0 Then
            Result = 62500
        Else
            Exclude(rid)
        End If

        Return result
    End Function

    <Calculation(Id:="f0c42450de974c81bf1a7f451e3a74d2")>
    Public Function POG_LD_P21_SecEmptyCohort3 As Decimal
        Dim result = Decimal.Zero
        Dim AcademyFilter As Decimal = F100_AllAcademies
        Print(AcademyFilter, "Filter", rid)
        If AcademyFilter > 0 Then
            Result = 93500
        Else
            Exclude(rid)
        End If

        Return result
    End Function

    <Calculation(Id:="ced528d1e33f420c9569cacfb96993d1")>
    Public Function POG_LD_P22_SecEmptyCohort4 As Decimal
        Dim result = Decimal.Zero
        Dim AcademyFilter As Decimal = F100_AllAcademies
        Print(AcademyFilter, "Filter", rid)
        If AcademyFilter > 0 Then
            Result = 125000
        Else
            Exclude(rid)
        End If

        Return result
    End Function

    <Calculation(Id:="452ca3edad374d828aca5406a4e17649")>
    Public Function POG_LD_P23_Sec_Max_Cap As Decimal
        Dim result = Decimal.Zero
        Dim AcademyFilter As Decimal = F100_AllAcademies
        Print(AcademyFilter, "Filter", rid)
        If AcademyFilter > 0 Then
            Result = 312000
        Else
            Exclude(rid)
        End If

        Return result
    End Function

    <Calculation(Id:="c384863aa18047d1b4bca48d12c1e746")>
    Public Function POG_LD_P24_AllThruEmptyCohort1 As Decimal
        Dim result = Decimal.Zero
        Dim AcademyFilter As Decimal = F100_AllAcademies
        Print(AcademyFilter, "Filter", rid)
        If AcademyFilter > 0 Then
            Result = 27000
        Else
            Exclude(rid)
        End If

        Return result
    End Function

    <Calculation(Id:="0adb17a45a2248f8b7a9ccdb56448aee")>
    Public Function POG_LD_P25_AllThruEmptyCohort2 As Decimal
        Dim result = Decimal.Zero
        Dim AcademyFilter As Decimal = F100_AllAcademies
        Print(AcademyFilter, "Filter", rid)
        If AcademyFilter > 0 Then
            Result = 40500
        Else
            Exclude(rid)
        End If

        Return result
    End Function

    <Calculation(Id:="c7e4d5e27abf471eac422a1cb7f2e91d")>
    Public Function POG_LD_P26_AllThruEmptyCohort3 As Decimal
        Dim result = Decimal.Zero
        Dim AcademyFilter As Decimal = F100_AllAcademies
        Print(AcademyFilter, "Filter", rid)
        If AcademyFilter > 0 Then
            Result = 54000
        Else
            Exclude(rid)
        End If

        Return result
    End Function

    <Calculation(Id:="89891e1480c246a6bc96d1607cd7df02")>
    Public Function POG_LD_P27_AllThruEmptyCohort4 As Decimal
        Dim result = Decimal.Zero
        Dim AcademyFilter As Decimal = F100_AllAcademies
        Print(AcademyFilter, "Filter", rid)
        If AcademyFilter > 0 Then
            Result = 62500
        Else
            Exclude(rid)
        End If

        Return result
    End Function

    <Calculation(Id:="e6dd93470d7a473aad60f2551d9d2a6e")>
    Public Function POG_LD_P28_AllThruEmptyCohort5 As Decimal
        Dim result = Decimal.Zero
        Dim AcademyFilter As Decimal = F100_AllAcademies
        Print(AcademyFilter, "Filter", rid)
        If AcademyFilter > 0 Then
            Result = 93500
        Else
            Exclude(rid)
        End If

        Return result
    End Function

    <Calculation(Id:="dde9a5d3820040628b178036d63b162a")>
    Public Function POG_LD_P29_AllThruEmptyCohorts6 As Decimal
        Dim result = Decimal.Zero
        Dim AcademyFilter As Decimal = F100_AllAcademies
        Print(AcademyFilter, "Filter", rid)
        If AcademyFilter > 0 Then
            Result = 125000
        Else
            Exclude(rid)
        End If

        Return result
    End Function

    <Calculation(Id:="9a212823429d4a0c99a1e619cc35a44c")>
    Public Function POG_LD_P30_AllThru_Max_Cap As Decimal
        Dim result = Decimal.Zero
        Dim AcademyFilter As Decimal = F100_AllAcademies
        Print(AcademyFilter, "Filter", rid)
        If AcademyFilter > 0 Then
            Result = 402500
        Else
            Exclude(rid)
        End If

        Return result
    End Function

    <Calculation(Id:="b84e7abeb5fe4a41861174b68afaee4f")>
    Public Function POG_LD_P31_1619FS_Year1 As Decimal
        Dim result = Decimal.Zero
        Dim AcademyFilter As Decimal = F100_AllAcademies
        Print(AcademyFilter, "Filter", rid)
        If AcademyFilter > 0 Then
            Result = 108000
        Else
            Exclude(rid)
        End If

        Return result
    End Function

    <Calculation(Id:="25f098d4aca04750b3255a6872324c30")>
    Public Function POG_LD_P32_1619FS_Year2 As Decimal
        Dim result = Decimal.Zero
        Dim AcademyFilter As Decimal = F100_AllAcademies
        Print(AcademyFilter, "Filter", rid)
        If AcademyFilter > 0 Then
            Result = 27000
        Else
            Exclude(rid)
        End If

        Return result
    End Function

    <Calculation(Id:="5cf8b160f20b491ca8034777920b9b55")>
    Public Function POG_LD_P33_UTC_Year1 As Decimal
        Dim result = Decimal.Zero
        Dim AcademyFilter As Decimal = F100_AllAcademies
        Print(AcademyFilter, "Filter", rid)
        If AcademyFilter > 0 Then
            Result = 157500
        Else
            Exclude(rid)
        End If

        Return result
    End Function

    <Calculation(Id:="a1f4d1f92693433a825a8ad9a1de93f1")>
    Public Function POG_LD_P34_UTC_Year2 As Decimal
        Dim result = Decimal.Zero
        Dim AcademyFilter As Decimal = F100_AllAcademies
        Print(AcademyFilter, "Filter", rid)
        If AcademyFilter > 0 Then
            Result = 94500
        Else
            Exclude(rid)
        End If

        Return result
    End Function

    <Calculation(Id:="ae1dae49dfa84c4dbc2026b84ee63344")>
    Public Function POG_LD_P35_UTC_Year3 As Decimal
        Dim result = Decimal.Zero
        Dim AcademyFilter As Decimal = F100_AllAcademies
        Print(AcademyFilter, "Filter", rid)
        If AcademyFilter > 0 Then
            Result = 63000
        Else
            Exclude(rid)
        End If

        Return result
    End Function

    <Calculation(Id:="5c8c8e5467294d01b5cd99c228d9b182")>
    Public Function POG_LD_P36_StudioSchool_Year1 As Decimal
        Dim result = Decimal.Zero
        Dim AcademyFilter As Decimal = F100_AllAcademies
        Print(AcademyFilter, "Filter", rid)
        If AcademyFilter > 0 Then
            Result = 90000
        Else
            Exclude(rid)
        End If

        Return result
    End Function

    <Calculation(Id:="a6f257de5a2a4c7ab0f56bd196e36d6d")>
    Public Function POG_LD_P37_StudioSchool_Year2 As Decimal
        Dim result = Decimal.Zero
        Dim AcademyFilter As Decimal = F100_AllAcademies
        Print(AcademyFilter, "Filter", rid)
        If AcademyFilter > 0 Then
            Result = 54000
        Else
            Exclude(rid)
        End If

        Return result
    End Function

    <Calculation(Id:="7b73197c0b4049d4a069829dade1e123")>
    Public Function POG_LD_P38_StudioSchool_Year3 As Decimal
        Dim result = Decimal.Zero
        Dim AcademyFilter As Decimal = F100_AllAcademies
        Print(AcademyFilter, "Filter", rid)
        If AcademyFilter > 0 Then
            Result = 36000
        Else
            Exclude(rid)
        End If

        Return result
    End Function

    <Calculation(Id:="bcb9cb2c616a49aea9d32ac70110e23f")>
    Public Function POG_LD_P39_SpecialSchool_Year1 As Decimal
        Dim result = Decimal.Zero
        Dim AcademyFilter As Decimal = F100_AllAcademies
        Print(AcademyFilter, "Filter", rid)
        If AcademyFilter > 0 Then
            Result = 85000
        Else
            Exclude(rid)
        End If

        Return result
    End Function

    <Calculation(Id:="9bcd879e7d1a4029b146467deff55062")>
    Public Function POG_LD_P40_SpecialSchool_Year2 As Decimal
        Dim result = Decimal.Zero
        Dim AcademyFilter As Decimal = F100_AllAcademies
        Print(AcademyFilter, "Filter", rid)
        If AcademyFilter > 0 Then
            Result = 51000
        Else
            Exclude(rid)
        End If

        Return result
    End Function

    <Calculation(Id:="c8dfe7ad4b6b4aab815c04a86ab4ec62")>
    Public Function POG_LD_P41_SpecialSchool_Year3 As Decimal
        Dim result = Decimal.Zero
        Dim AcademyFilter As Decimal = F100_AllAcademies
        Print(AcademyFilter, "Filter", rid)
        If AcademyFilter > 0 Then
            Result = 34000
        Else
            Exclude(rid)
        End If

        Return result
    End Function

    <Calculation(Id:="a07aa8f880014bfeaa927679604bfc66")>
    Public Function SUG_P03_Class_Size As Decimal
        Dim result = Decimal.Zero
        Dim AcademyFilter As Decimal = F100_AllAcademies
        Print(AcademyFilter, "Filter", rid)
        If AcademyFilter > 0 Then
            Result = 30
        Else
            Exclude(rid)
        End If

        Return result
    End Function

    <Calculation(Id:="e53d3f001cfb4d0fb4784e030055a6db")>
    Public Function SUG_P05_Pri_SUGA_Min As Decimal
        Dim result = Decimal.Zero
        Dim AcademyFilter As Decimal = F100_AllAcademies
        Print(AcademyFilter, "Filter", rid)
        If AcademyFilter > 0 Then
            Result = 25000
        Else
            Exclude(rid)
        End If

        Return result
    End Function

    <Calculation(Id:="f2882a9873ed4da38d091bf4edd9e22f")>
    Public Function SUG_P06_Pri_SUGA_Max As Decimal
        Dim result = Decimal.Zero
        Dim AcademyFilter As Decimal = F100_AllAcademies
        Print(AcademyFilter, "Filter", rid)
        If AcademyFilter > 0 Then
            Result = 50000
        Else
            Exclude(rid)
        End If

        Return result
    End Function

    <Calculation(Id:="44cd9bc88e434c18b3c3116ad807dffb")>
    Public Function SUGA_P08_Sec_Per_Pupil_Funding_Rate As Decimal
        Dim result = Decimal.Zero
        Dim AcademyFilter As Decimal = F100_AllAcademies
        Print(AcademyFilter, "Filter", rid)
        If AcademyFilter > 0 Then
            Result = 150
        Else
            Exclude(rid)
        End If

        Return result
    End Function

    <Calculation(Id:="b07c1b090f89499eb1450bb208c24513")>
    Public Function SUGA_P08a_Per_Place_Funding_Rate As Decimal
        Dim result = Decimal.Zero
        Dim AcademyFilter As Decimal = F100_AllAcademies
        Print(AcademyFilter, "Filter", rid)
        If AcademyFilter > 0 Then
            Result = 250
        Else
            Exclude(rid)
        End If

        Return result
    End Function

    <Calculation(Id:="8726ea489155432d840dae509f37062e")>
    Public Function SUGB_P12_Pri_MinCap As Decimal
        Dim result = Decimal.Zero
        Dim AcademyFilter As Decimal = F100_AllAcademies
        Print(AcademyFilter, "Filter", rid)
        If AcademyFilter > 0 Then
            Result = 10000
        Else
            Exclude(rid)
        End If

        Return result
    End Function

    <Calculation(Id:="3d712a5566d3447d9b3802308531c4ac")>
    Public Function SUGB_P13_Pri_MaxCap As Decimal
        Dim result = Decimal.Zero
        Dim AcademyFilter As Decimal = F100_AllAcademies
        Print(AcademyFilter, "Filter", rid)
        If AcademyFilter > 0 Then
            Result = 50000
        Else
            Exclude(rid)
        End If

        Return result
    End Function

    <Calculation(Id:="d15d4026218145ea9272de9c282a456c")>
    Public Function SUGB_P14_PerPercentRate As Decimal
        Dim result = Decimal.Zero
        Dim AcademyFilter As Decimal = F100_AllAcademies
        Print(AcademyFilter, "Filter", rid)
        If AcademyFilter > 0 Then
            Result = 2500
        Else
            Exclude(rid)
        End If

        Return result
    End Function

    <Calculation(Id:="f62397c70d80471a97f8f6f85d999fb6")>
    Public Function SUGB_P15_CapacityPerCentMin As Decimal
        Dim result = Decimal.Zero
        Dim AcademyFilter As Decimal = F100_AllAcademies
        Print(AcademyFilter, "Filter", rid)
        If AcademyFilter > 0 Then
            Result = 0.9
        Else
            Exclude(rid)
        End If

        Return result
    End Function

    <Calculation(Id:="72a24329e524468bb4a1e00456707273")>
    Public Function SUGB_P16_FundingPerCentMin As Decimal
        Dim result = Decimal.Zero
        Dim AcademyFilter As Decimal = F100_AllAcademies
        Print(AcademyFilter, "Filter", rid)
        If AcademyFilter > 0 Then
            Result = 0.865
        Else
            Exclude(rid)
        End If

        Return result
    End Function

    <Calculation(Id:="e2bc85a823d84ee29e4aee829c73029c")>
    Public Function SUGB_P22_SecMinCap As Decimal
        Dim result = Decimal.Zero
        Dim AcademyFilter As Decimal = F100_AllAcademies
        Print(AcademyFilter, "Filter", rid)
        If AcademyFilter > 0 Then
            Result = 10000
        Else
            Exclude(rid)
        End If

        Return result
    End Function

    <Calculation(Id:="4b9c427b13004d6582c6679bf5648f1f")>
    Public Function SUGB_P23_SecMaxCapSmall As Decimal
        Dim result = Decimal.Zero
        Dim AcademyFilter As Decimal = F100_AllAcademies
        Print(AcademyFilter, "Filter", rid)
        If AcademyFilter > 0 Then
            Result = 60000
        Else
            Exclude(rid)
        End If

        Return result
    End Function

    <Calculation(Id:="ea9156b96d5f4a0ba6b116ca59e8b2ce")>
    Public Function SUGB_P24_SecMaxCapLarge As Decimal
        Dim result = Decimal.Zero
        Dim AcademyFilter As Decimal = F100_AllAcademies
        Print(AcademyFilter, "Filter", rid)
        If AcademyFilter > 0 Then
            Result = 80000
        Else
            Exclude(rid)
        End If

        Return result
    End Function

    <Calculation(Id:="86f87425cf3f4abfa04d10392e88836b")>
    Public Function SUGB_P25_PerPercentRateSmall As Decimal
        Dim result = Decimal.Zero
        Dim AcademyFilter As Decimal = F100_AllAcademies
        Print(AcademyFilter, "Filter", rid)
        If AcademyFilter > 0 Then
            Result = 3000
        Else
            Exclude(rid)
        End If

        Return result
    End Function

    <Calculation(Id:="43a0c51ba6db4adbaf42c24703809742")>
    Public Function SUGB_P26_PerPercentRateLarge As Decimal
        Dim result = Decimal.Zero
        Dim AcademyFilter As Decimal = F100_AllAcademies
        Print(AcademyFilter, "Filter", rid)
        If AcademyFilter > 0 Then
            Result = 4000
        Else
            Exclude(rid)
        End If

        Return result
    End Function

    <Calculation(Id:="3a8ca4f5283d4843a9614b70b449be2d")>
    Public Function SUGB_P32_MaxCapSmall As Decimal
        Dim result = Decimal.Zero
        Dim AcademyFilter As Decimal = F100_AllAcademies
        Print(AcademyFilter, "Filter", rid)
        If AcademyFilter > 0 Then
            Result = 250
        Else
            Exclude(rid)
        End If

        Return result
    End Function

    <Calculation(Id:="cb2ead62b8b041d79fb0be3b7aba1790")>
    Public Function SUGB_P33_MinCapLarge As Decimal
        Dim result = Decimal.Zero
        Dim AcademyFilter As Decimal = F100_AllAcademies
        Print(AcademyFilter, "Filter", rid)
        If AcademyFilter > 0 Then
            Result = 1000
        Else
            Exclude(rid)
        End If

        Return result
    End Function

    <Calculation(Id:="2442b91c63174ae7907c4846860ea065")>
    Public Function SUGB_P34_MedPupilRange As Decimal
        Dim result = Decimal.Zero
        Dim AcademyFilter As Decimal = F100_AllAcademies
        Print(AcademyFilter, "Filter", rid)
        If AcademyFilter > 0 Then
            Result = 750
        Else
            Exclude(rid)
        End If

        Return result
    End Function

    <Calculation(Id:="90c1a7c3ea244c6ca811d1620a0f55b4")>
    Public Function SUGB_P35_MaxCapDiff As Decimal
        Dim result = Decimal.Zero
        Dim AcademyFilter As Decimal = F100_AllAcademies
        Print(AcademyFilter, "Filter", rid)
        If AcademyFilter > 0 Then
            Result = 20100
        Else
            Exclude(rid)
        End If

        Return result
    End Function

    <Calculation(Id:="047b7514056f4fcdba9439822aa00f21")>
    Public Function SUGB_P36_DiseconPerCentDiff As Decimal
        Dim result = Decimal.Zero
        Dim AcademyFilter As Decimal = F100_AllAcademies
        Print(AcademyFilter, "Filter", rid)
        If AcademyFilter > 0 Then
            Result = 20
        Else
            Exclude(rid)
        End If

        Return result
    End Function

    <Calculation(Id:="c552310bca6f4fc19c940971c5f42bfa")>
    Public Function HN_Unit_Flag As Decimal
        Dim result As Decimal = 0
        Dim HN_UNIT As String = Datasets.APTLocalfactorsdataset.HighNeedsUnit
        Dim AcadFilter As Decimal = F100_AllAcademies
        If HN_UNIT isnot nothing then
            If AcadFilter = 17181 then
                If HN_UNIT.Contains("Yes") Then
                    result = 1
                Else
                    Result = 0
                End If
            Else
                Exclude(rid)
            End If
        End If

        Print(HN_UNIT, "HN UNIT", rid)
        Return result
    End Function

    <Calculation(Id:="f6ba5becac3c4c6e9df798280a870244")>
    Public Function Reception_Uplift_Flag As Decimal
        Dim Result = 0
        Dim RU_Applicable As String = LAtoProv(Datasets.APTProformadataset.ReceptionUpliftYesNo)
        Print(RU_Applicable, "Recep Uplift", rid)
        If RU_Applicable = "Yes" Then
            Result = 1
        ElseIf RU_Applicable = "NO" Then
            Result = 0
        Else
            exclude(rid)
        End If

        If Result = 0 Then
            Exclude(rid)
        End If

        Return Result
    End Function

    <Calculation(Id:="0cabd4d4185847a4844864a2afccc2a9")>
    Public Function NOR_P01_RU As Decimal
        Dim result = Decimal.Zero
        Dim RU_Flag as Single = LAtoProv(Reception_Uplift_Flag)
        Dim IsNull As Boolean
        IsNull = IIf(Datasets.APTInputsandAdjustments.ReceptionUplift, false, true)
        Dim RU_InpAdj As Decimal = Datasets.APTInputsandAdjustments.ReceptionUplift
        Dim RU_APT As Decimal = Datasets.APTAdjustedFactorsdataset.ReceptionDifference
        Dim RU_Census As Decimal = Datasets.CensusNumberCounts.ReceptionDifference
        Dim RU_RFDC As Decimal = Datasets.EstimateNumberCounts.EstReceptionDifference
        Dim NOR_RFDC As Decimal = Datasets.EstimateNumberCounts.EstNOR
        Dim Guaranteed As String = Datasets.APTInputsandAdjustments.Amendedpupilnumbersguaranteed ? 
        Dim NOR_InpAdj As Decimal = Datasets.APTInputsandAdjustments.NOR
        Dim F100_AllAcademies As Decimal = F100_AllAcademies
        Dim F900_FundingBasis As Decimal = F900_FundingBasis
        If currentscenario.periodid = 2017181 And (F100_AllAcademies = 17181) Then
            If RU_Flag = 1 And F900_FundingBasis = 1 And IsNull = False Then
                Result = RU_InpAdj
            ElseIf RU_Flag = 1 And F900_FundingBasis = 1 And IsNull = True Then
                Result = RU_Census
            ElseIf RU_Flag = 1 And F900_FundingBasis = 2 And Guaranteed = "YES" And NOR_InpAdj > NOR_RFDC Then
                Result = RU_InpAdj
            ElseIf RU_Flag = 1 And F900_FundingBasis = 2 Then
                Result = RU_RFDC
            End If
        Else If currentscenario.periodid = 2017181 And (F100_AllAcademies = 17182 Or F100_AllAcademies = 17183) Then
            If RU_Flag = 1 And F900_FundingBasis = 1 Then
                Result = RU_APT
            ElseIf RU_Flag = 1 And F900_FundingBasis = 2 And Guaranteed = "YES" And NOR_InpAdj > NOR_RFDC Then
                Result = RU_InpAdj
            ElseIf RU_Flag = 1 And F900_FundingBasis = 2 Then
                Result = RU_RFDC
            End If
        Else
            Exclude(rid)
        End If

        Print(F100_AllAcademies, "F100_AllAcademies", rid)
        Print(RU_Flag, "RU_Flag", rid)
        Print(F900_FundingBasis, "F900_FundingBasis", rid)
        Print(RU_InpAdj, "Inp And Adj RU", rid)
        return result
    End Function

    <Calculation(Id:="89b68a96a15e49c58e16f76588dcbc45")>
    Public Function NOR_P02_PRI As Decimal
        Dim result = Decimal.Zero
        Dim IsNull As Boolean = iif(Datasets.APTInputsandAdjustments.NORPrimary, false, true)
        Dim RU_RFDC As Decimal = Datasets.EstimateNumberCounts.EstReceptionDifference
        Dim NOR_P001 As Decimal = P001_NOR_Est_Pri
        Dim NOR_P008 As Decimal = P008_NOR_Est_RtoY11
        Dim Guaranteed As String = Datasets.APTInputsandAdjustments.Amendedpupilnumbersguaranteed ? 
        Dim NOR_InpAdj As Decimal = Datasets.APTInputsandAdjustments.NOR
        Dim NOR_Pri_InpAdj As Decimal = Datasets.APTInputsandAdjustments.NORPrimary
        Dim NOR_Pri_Census As Decimal = Datasets.CensusNumberCounts.NORPrimary
        Dim F100_AllAcademies As Decimal = F100_AllAcademies
        Dim F900_FundingBasis As Decimal = F900_FundingBasis
        Dim NOR_Pri_APT_Adj As Decimal = Datasets.APTAdjustedFactorsdataset.NORPrimary
        Dim NOR_P01_RU As Decimal = Products .1617_ NOR.NOR_P01_RU 
        If currentscenario.periodid = 2017181 And (F100_AllAcademies = 17181) Then
            If F900_FundingBasis = 1 And IsNull = False Then
                Result = NOR_Pri_InpAdj
            ElseIf F900_FundingBasis = 1 And IsNull = True Then
                Result = NOR_Pri_Census
            ElseIf F900_FundingBasis = 2 And Guaranteed = "YES" And NOR_InpAdj > NOR_P008 Then
                Result = NOR_Pri_InpAdj
            ElseIf F900_FundingBasis = 2 Then
                Result = NOR_P001
            End If
        Else If currentscenario.periodid = 2017181 And (F100_AllAcademies = 17182 Or F100_AllAcademies = 17183) Then
            If F900_FundingBasis = 1 Then
                Result = NOR_Pri_APT_Adj - NOR_P01_RU
            ElseIf F900_FundingBasis = 2 And Guaranteed = "YES" And NOR_InpAdj > NOR_P008 Then
                Result = NOR_Pri_InpAdj
            ElseIf F900_FundingBasis = 2 Then
                Result = NOR_P001
            End If
        Else
            Exclude(rid)
        End If

        return result
    End Function

    <Calculation(Id:="a9e6fda418d6490fb7524be4930e9605")>
    Public Function NOR_P03_Y1Y4 As Decimal
        Dim result = Decimal.Zero
        Dim IsNull As Boolean
        IsNull = IIf(Datasets.APTInputsandAdjustments.NORYr14, false, true)
        Dim DateOpened as date = Datasets.ProviderInformation.DateOpened
        Dim NOR_Y1Y4_Census As Decimal = Datasets.CensusNumberCounts.NORY14
        Dim NOR_Y1Y4_InpAdj As Decimal = Datasets.APTInputsandAdjustments.NORYr14
        Dim NOR_InpAdj As Decimal = Datasets.APTInputsandAdjustments.NOR
        Dim NOR_Y1Y4_Est As Decimal = Datasets.EstimateNumberCounts.EstNORY14
        Dim NOR_Y1Y4_APT As Decimal = Datasets.APTAdjustedFactorsdataset.NOR14forcalculationoftheeligiblepupilsfortheprimarypriorattainmentfactor
        Dim NOR_P008 As double = P008_NOR_Est_RtoY11
        Dim NOR_P002 As double = P002_NOR_Est_Y1to4
        Dim Guaranteed As String = Datasets.APTInputsandAdjustments.Amendedpupilnumbersguaranteed ? 
        Dim F100_AllAcademies As Decimal = F100_AllAcademies
        Dim F900_FundingBasis As String = F900_FundingBasis
        Print(IsNull, "IsNull", rid)
        Print(NOR_InpAdj, "NOR InputsAdj", rid)
        Print(NOR_Y1Y4_Census, "NORY1Y4 Census", rid)
        Print(NOR_P002, "NORY1Y4 Estimate", rid)
        Print(F900_FundingBasis, "F900_FundingBasis", rid)
        If currentscenario.periodid = 2017181 And F100_AllAcademies = 17181 then
            If F900_FundingBasis = 1 And IsNull = False Then
                Result = NOR_Y1Y4_InpAdj
            ElseIf F900_FundingBasis = 1 And IsNull = True Then
                Result = NOR_Y1Y4_Census
            ElseIf F900_FundingBasis = 2 And Guaranteed = "YES" And NOR_InpAdj > NOR_P008 Then
                Result = NOR_Y1Y4_InpAdj
            ElseIf F900_FundingBasis = 2 Then
                Result = NOR_P002
            End If
        ElseIf currentscenario.periodid = 2017181 And (F100_AllAcademies = 17182 Or F100_AllAcademies = 17183) Then
            If F900_FundingBasis = 1 Then
                Result = NOR_Y1Y4_APT
            ElseIf F900_FundingBasis = 2 And Guaranteed = "YES" And NOR_InpAdj > NOR_P008 Then
                Result = NOR_Y1Y4_InpAdj
            ElseIf F900_FundingBasis = 2 Then
                Result = NOR_P002
            End If
        Else
            Exclude(rid)
        End If

        Return result
    End Function

    <Calculation(Id:="3c2c967da96d45c29d5d08cef2c9309b")>
    Public Function NOR_P04_Y5Y6 As Decimal
        Dim result = Decimal.Zero
        Dim IsNull As Boolean
        IsNull = IIf(Datasets.APTInputsandAdjustments.NORYr56, false, true)
        Dim NOR_Y5Y6_InpAdj As Decimal = Datasets.APTInputsandAdjustments.NORYr56
        Dim DateOpened as date = Datasets.ProviderInformation.DateOpened
        Dim NOR_Y5Y6_Census As Decimal = Datasets.CensusNumberCounts.NORY56
        Dim NOR_P008 As Decimal = P008_NOR_Est_RtoY11
        Dim NOR_P003 As Decimal = P003_NOR_Est_Y5to6
        Dim NOR_Y5Y6_APT As Decimal = Datasets.APTAdjustedFactorsdataset.NOR56forcalculationoftheeligiblepupilsfortheprimarypriorattainmentfactor
        Dim Guaranteed As String = Datasets.APTInputsandAdjustments.Amendedpupilnumbersguaranteed ? 
        Dim NOR_InpAdj As Decimal = Datasets.APTInputsandAdjustments.NOR
        Dim F100_AllAcademies = F100_AllAcademies
        Dim F900_FundingBasis As String = F900_FundingBasis
        Print(IsNull, "IsNull", rid)
        Print(NOR_InpAdj, "NOR Inp Adj", rid)
        Print(NOR_Y5Y6_Census, "NOR Y5Y6 Census", rid)
        Print(NOR_P003, "NOR Y5Y6 Estimate", rid)
        Print(F900_FundingBasis, "F900_FundingBasis", rid)
        If currentscenario.periodid = 2017181 And (F100_AllAcademies = 17181) Then
            If F900_FundingBasis = 1 And IsNull = False Then
                Result = NOR_Y5Y6_InpAdj
            ElseIf F900_FundingBasis = 1 And IsNull = True Then
                Result = NOR_Y5Y6_Census
            ElseIf F900_FundingBasis = 2 And Guaranteed = "YES" And NOR_InpAdj > NOR_P008 Then
                Result = NOR_Y5Y6_InpAdj
            ElseIf F900_FundingBasis = 2 Then
                Result = NOR_P003
            Else
                Result = 0
            End If
        ElseIf currentscenario.periodid = 2017181 And (F100_AllAcademies = 17182 Or F100_AllAcademies = 17183) Then
            If F900_FundingBasis = 1 Then
                Result = NOR_Y5Y6_APT
            ElseIf F900_FundingBasis = 2 And Guaranteed = "YES" And NOR_InpAdj > NOR_P008 Then
                Result = NOR_Y5Y6_InpAdj
            ElseIf F900_FundingBasis = 2 Then
                Result = NOR_P003
            End If
        Else
            Exclude(rid)
        End If

        Return result
    End Function

    <Calculation(Id:="8e7c02c3717642eca4697e8fa4bf98c9")>
    Public Function NOR_P05_NUR As Decimal
        Dim result = Decimal.Zero
        Dim IsNull As Boolean
        Dim NOR_Nursery_Census = Datasets.CensusNumberCounts.FTENursery
        Dim NOR_Nursery_RFDC = Datasets.EstimateNumberCounts.EstFTENursery
        Dim F100_AllAcademies = F100_AllAcademies
        Dim F900_FundingBasis = F900_FundingBasis
        Print(F900_FundingBasis, "F900_FundingBasis", rid)
        Print(NOR_Nursery_Census, "Nursery_NOR", rid)
        Print(NOR_Nursery_RFDC, "Nur_est", rid)
        If currentscenario.periodid = 2017181 And F100_AllAcademies = 17181 Then
            If F900_FundingBasis = 1 And ISNULL = False Then
                result = NOR_Nursery_Census
            ElseIf F900_FundingBasis = 2 Then
                result = NOR_Nursery_RFDC
            End If
        ElseIf currentscenario.periodid = 2017181 And (F100_AllAcademies = 17182 Or F100_AllAcademies = 17183) Then
            If F900_FundingBasis = 1 Then
                Result = NOR_Nursery_Census
            Else If F900_FundingBasis = 2 Then
                Result = NOR_Nursery_RFDC
            Else
                exclude(rid)
            End If
        Else
            Exclude(rid)
        End If

        Return result
    End Function

    <Calculation(Id:="d36417f06dda4eebae1435ea2c27f84e")>
    Public Function NOR_P06_SEC As Decimal
        Dim result = Decimal.Zero
        Dim NOR_P07 As Decimal = NOR_P07_KS3
        Dim NOR_P08 As Decimal = NOR_P08_KS4
        Dim F100_AllAcademies As Decimal = F100_AllAcademies
        Dim F900_FundingBasis As Decimal = F900_FundingBasis
        If currentscenario.periodid = 2017181 And (F100_AllAcademies = 17181 Or F100_AllAcademies = 17182 Or F100_AllAcademies = 17183) then
            result = NOR_P07 + NOR_P08
        Else
            Exclude(rid)
        End If

        Print(F900_FundingBasis, "F900_FundingBasis", rid)
        Print(F100_AllAcademies, "F100_AllAcademies", rid)
        Return Result
    End Function

    <Calculation(Id:="1aa0065af37c436191cd14c9767d2e53")>
    Public Function NOR_P07_KS3 As Decimal
        Dim result = Decimal.Zero
        Dim IsNull As Boolean = iif(Datasets.APTInputsandAdjustments.NORKS3, false, true)
        Dim DateOpened As Date = Datasets.ProviderInformation.DateOpened
        Dim NOR_KS3_InpAdj As Decimal = Datasets.APTInputsandAdjustments.NORKS3
        Dim NOR_InpAdj As Decimal = Datasets.APTInputsandAdjustments.NOR
        Dim NOR_KS3_Cen As Decimal = Datasets.CensusNumberCounts.NORKS3
        Dim NOR_P006 As Decimal = P006_NOR_Est_KS3
        Dim NOR_P008 As Decimal = P008_NOR_Est_RtoY11
        Dim NOR_KS3_AdjFact As Decimal = Datasets.APTAdjustedFactorsdataset.NORKS3
        Dim Guaranteed As String = Datasets.APTInputsandAdjustments.Amendedpupilnumbersguaranteed ? 
        Dim F100_AllAcademies As Decimal = F100_AllAcademies
        Dim F900_FundingBasis As Decimal = F900_FundingBasis
        Print(NOR_InpAdj, "NOR InpAdj", rid)
        Print(DateOpened, "Date Opened", rid)
        Print(F900_FundingBasis, "F900_FundingBasis", rid)
        Print(NOR_KS3_Cen, "NOR_KS3_Cen", rid)
        Print(NOR_P008, "NOR_KS3_Est", rid)
        Print(F100_AllAcademies, "F100_AllAcademies", rid)
        If currentscenario.periodid = 2017181 And F100_AllAcademies = 17181 then
            If F900_FundingBasis = 1 And ISNULL = False Then
                Result = NOR_KS3_InpAdj
            ElseIf F900_FundingBasis = 1 And ISNULL = True Then
                Result = NOR_KS3_Cen
            ElseIf F900_FundingBasis = 2 And Guaranteed = "YES" And NOR_InpAdj > NOR_P008 Then
                Result = NOR_KS3_InpAdj
            ElseIf F900_FundingBasis = 2 Then
                Result = NOR_P006
            End If
        ElseIf currentscenario.periodid = 2017181 And (F100_AllAcademies = 17182 Or F100_AllAcademies = 17183) Then
            If F900_FundingBasis = 1 Then
                Result = NOR_KS3_AdjFact
            ElseIf F900_FundingBasis = 2 And Guaranteed = "YES" And NOR_InpAdj > NOR_P008 Then
                Result = NOR_KS3_InpAdj
            ElseIf F900_FundingBasis = 2 Then
                Result = NOR_P006
            End If
        Else
            Exclude(rid)
        End If

        Return Result
    End Function

    <Calculation(Id:="446b133077794c0f8ec76ad7594cab40")>
    Public Function NOR_P08_KS4 As Decimal
        Dim result = Decimal.Zero
        Dim DateOpened As Date = Datasets.ProviderInformation.DateOpened
        Dim IsNull As Boolean
        IsNull = IIf(Datasets.APTInputsandAdjustments.NORKS4, false, true)
        Dim NOR_KS4_InpAdj As Decimal = Datasets.APTInputsandAdjustments.NORKS4
        Dim NOR_InpAdj As Decimal = Datasets.APTInputsandAdjustments.NOR
        Dim NOR_KS4_Cen As Decimal = Datasets.CensusNumberCounts.NORKS4
        Dim NOR_P007 As Decimal = P007_NOR_Est_KS4
        Dim NOR_P008 As Decimal = P008_NOR_Est_RtoY11
        Dim NOR_KS4_AdjFact As Decimal = Datasets.APTAdjustedFactorsdataset.NORKS4
        Dim Guaranteed As String = Datasets.APTInputsandAdjustments.Amendedpupilnumbersguaranteed ? 
        Dim F100_AllAcademies As Decimal = F100_AllAcademies
        Dim F900_FundingBasis As Decimal = F900_FundingBasis
        Print(IsNull, "IsNull", rid)
        Print(NOR_InpAdj, "NOR InpAdj", rid)
        Print(DateOpened, "Date Opened", rid)
        Print(F900_FundingBasis, "F900_FundingBasis", rid)
        Print(NOR_KS4_Cen, "NOR_KS4_Cen", rid)
        Print(NOR_P008, "NOR_KS4_Est", rid)
        Print(F100_AllAcademies, "F100_AllAcademies", rid)
        If currentscenario.periodid = 2017181 And F100_AllAcademies = 17181 then
            If F900_FundingBasis = 1 And ISNULL = False Then
                Result = NOR_KS4_InpAdj
            ElseIf F900_FundingBasis = 1 And ISNULL = True Then
                Result = NOR_KS4_Cen
            ElseIf F900_FundingBasis = 2 And Guaranteed = "YES" And NOR_InpAdj > NOR_P008 Then
                Result = NOR_KS4_InpAdj
            ElseIf F900_FundingBasis = 2 Then
                Result = NOR_P007
            End If
        ElseIf currentscenario.periodid = 2017181 And (F100_AllAcademies = 17182 Or F100_AllAcademies = 17183) Then
            If F900_FundingBasis = 1 Then
                Result = NOR_KS4_AdjFact
            ElseIf F900_FundingBasis = 2 And Guaranteed = "YES" And NOR_InpAdj > NOR_P008 Then
                Result = NOR_KS4_InpAdj
            ElseIf F900_FundingBasis = 2 Then
                Result = NOR_P007
            End If
        Else
            Exclude(rid)
        End If

        Return Result
    End Function

    <Calculation(Id:="2966289b71074759b98c58fc0a577a84")>
    Public Function NOR_P09_APT_HN_PRI As Decimal
        Dim result = Decimal.Zero
        Dim NOR_APT_HN_Pri As Decimal = Datasets.APTLocalfactorsdataset.NumberofprimarypupilsonrollattheschoolinHighNeedsplacesin201617
        Dim F100_AllAcademies As Decimal = F100_AllAcademies
        Print(NOR_APT_HN_Pri, "APT HN Pri", rid)
        If currentscenario.periodid = 2017181 And (F100_AllAcademies = 17181 Or F100_AllAcademies = 17182 Or F100_AllAcademies = 17183) then
            result = NOR_APT_HN_Pri
        Else
            Exclude(rid)
        End if

        Return result
    End Function

    <Calculation(Id:="49c45ef61b754a2da94133c662cc54d5")>
    Public Function NOR_P10_APT_HN_KS3 As Decimal
        Dim result = Decimal.Zero
        Dim NOR_APT_HN_KS3 As Decimal = Datasets.APTLocalfactorsdataset.NumberofKS3pupilsonrollattheschoolinHighNeedsplacesin201617
        Dim F100_AllAcademies As Decimal = F100_AllAcademies
        If currentscenario.periodid = 2017181 And (F100_AllAcademies = 17181 Or F100_AllAcademies = 17182 Or F100_AllAcademies = 17183) then
            result = NOR_APT_HN_KS3
        Else
            Exclude(rid)
        End if

        return Result
    End Function

    <Calculation(Id:="436f9a8540c24189b11fa097a0fdb008")>
    Public Function NOR_P11_APT_HN_KS4 As Decimal
        Dim result = Decimal.Zero
        Dim NOR_APT_HN_KS4 As Decimal = Datasets.APTLocalfactorsdataset.NumberofKS4pupilsonrollattheschoolinHighNeedsplacesin201617
        Dim F100_AllAcademies = F100_AllAcademies
        Print(NOR_APT_HN_KS4, "APT_HN_HN_KS4", rid)
        If currentscenario.periodid = 2017181 And (F100_AllAcademies = 17181 Or F100_AllAcademies = 17182 Or F100_AllAcademies = 17183) then
            result = NOR_APT_HN_KS4
        Else
            Exclude(rid)
        End if

        return Result
    End Function

    <Calculation(Id:="6b434062edb24d03975dc001a8b5c12e")>
    Public Function NOR_P12_HND_HNP_HN_PRI As Decimal
        Dim result = Decimal.Zero
        Dim HNs_Places_Total As Decimal = Datasets.HighNeedsPlaces.Totalpre16HNsplaces
        Dim NOR_P02_PRI As Decimal = NOR_P02_PRI
        Dim NOR_P07_KS3 As Decimal = NOR_P07_KS3
        Dim NOR_P08_KS4 As Decimal = NOR_P08_KS4
        Dim F100_AllAcademies As Decimal = F100_AllAcademies
        Dim Pri_Unadj_Split As Decimal = math.round(HNs_Places_Total * NOR_P02_PRI / (NOR_P02_PRI + NOR_P07_KS3 + NOR_P08_KS4), 0)
        Dim KS3_Unadj_Split As Decimal = math.round(HNs_Places_Total * NOR_P07_KS3 / (NOR_P02_PRI + NOR_P07_KS3 + NOR_P08_KS4), 0)
        Dim KS4_Unadj_Split As Decimal = math.round(HNs_Places_Total * NOR_P08_KS4 / (NOR_P02_PRI + NOR_P07_KS3 + NOR_P08_KS4), 0)
        Dim Total_Unadj_Split As Decimal = Pri_Unadj_Split + KS3_Unadj_Split + KS4_Unadj_Split
        Dim Diff As Decimal = Total_Unadj_Split - HNs_Places_Total
        If KS3_Unadj_Split = 0 And KS4_Unadj_Split = 0 And (Diff < 0 Or Diff > 0) Then
            Result = Pri_Unadj_Split - Diff
        Else
            Result = Pri_Unadj_Split
        End If

        Return result
    End Function

    <Calculation(Id:="4a1b7b120b6f450b89a8757fdcf6d739")>
    Public Function NOR_P13_HND_HNP_HN_KS3 As Decimal
        Dim result = Decimal.Zero
        Dim HNs_Places_Total As Decimal = Datasets.HighNeedsPlaces.Totalpre16HNsplaces
        Dim NOR_P02_PRI As Decimal = NOR_P02_PRI
        Dim NOR_P07_KS3 As Decimal = NOR_P07_KS3
        Dim NOR_P08_KS4 As Decimal = NOR_P08_KS4
        Dim F100_AllAcademies As Decimal = F100_AllAcademies
        Dim Pri_Unadj_Split As Decimal = math.round(HNs_Places_Total * NOR_P02_PRI / (NOR_P02_PRI + NOR_P07_KS3 + NOR_P08_KS4), 0)
        Dim KS3_Unadj_Split As Decimal = math.round(HNs_Places_Total * NOR_P07_KS3 / (NOR_P02_PRI + NOR_P07_KS3 + NOR_P08_KS4), 0)
        Dim KS4_Unadj_Split As Decimal = math.round(HNs_Places_Total * NOR_P08_KS4 / (NOR_P02_PRI + NOR_P07_KS3 + NOR_P08_KS4), 0)
        Dim Total_Unadj_Split As Decimal = Pri_Unadj_Split + KS3_Unadj_Split + KS4_Unadj_Split
        Dim Diff As Decimal = Total_Unadj_Split - HNs_Places_Total
        If KS3_Unadj_Split > 0 And KS4_Unadj_Split = 0 And (Diff < 0 Or Diff > 0) Then
            Result = KS3_Unadj_Split - Diff
        Else
            Result = KS3_Unadj_Split
        End If

        Return result
    End Function

    <Calculation(Id:="5726ea3451e644a2bd5108bec4ec03f4")>
    Public Function NOR_P14_HND_HNP_HN_KS4 As Decimal
        Dim result = Decimal.Zero
        Dim HNs_Places_Total As Decimal = Datasets.HighNeedsPlaces.Totalpre16HNsplaces
        Dim NOR_P02_PRI As Decimal = NOR_P02_PRI
        Dim NOR_P07_KS3 As Decimal = NOR_P07_KS3
        Dim NOR_P08_KS4 As Decimal = NOR_P08_KS4
        Dim F100_AllAcademies As Decimal = F100_AllAcademies
        Dim Pri_Unadj_Split As Decimal = math.round(HNs_Places_Total * NOR_P02_PRI / (NOR_P02_PRI + NOR_P07_KS3 + NOR_P08_KS4), 0)
        Dim KS3_Unadj_Split As Decimal = math.round(HNs_Places_Total * NOR_P07_KS3 / (NOR_P02_PRI + NOR_P07_KS3 + NOR_P08_KS4), 0)
        Dim KS4_Unadj_Split As Decimal = math.round(HNs_Places_Total * NOR_P08_KS4 / (NOR_P02_PRI + NOR_P07_KS3 + NOR_P08_KS4), 0)
        Dim Total_Unadj_Split As Decimal = Pri_Unadj_Split + KS3_Unadj_Split + KS4_Unadj_Split
        Dim Diff As Decimal = Total_Unadj_Split - HNs_Places_Total
        If KS4_Unadj_Split > 0 And (Diff < 0 Or Diff > 0) Then
            Result = KS4_Unadj_Split - Diff
        Else
            Result = KS4_Unadj_Split
        End If

        Return result
    End Function

    <Calculation(Id:="ee38859654084a7bb41acbe2da959084")>
    Public Function NOR_P15_HND_HNP_AP_PRI As Decimal
        Dim result = Decimal.Zero
        Dim AP_Places_Total As Decimal = Datasets.HighNeedsPlaces.Totalpre16APplaces
        Dim NOR_P02_PRI As Decimal = NOR_P02_PRI
        Dim NOR_P07_KS3 As Decimal = NOR_P07_KS3
        Dim NOR_P08_KS4 As Decimal = NOR_P08_KS4
        Dim F100_AllAcademies As Decimal = F100_AllAcademies
        Dim Pri_Unadj_Split As Decimal = math.round(AP_Places_Total * NOR_P02_PRI / (NOR_P02_PRI + NOR_P07_KS3 + NOR_P08_KS4), 0)
        Dim KS3_Unadj_Split As Decimal = math.round(AP_Places_Total * NOR_P07_KS3 / (NOR_P02_PRI + NOR_P07_KS3 + NOR_P08_KS4), 0)
        Dim KS4_Unadj_Split As Decimal = math.round(AP_Places_Total * NOR_P08_KS4 / (NOR_P02_PRI + NOR_P07_KS3 + NOR_P08_KS4), 0)
        Dim Total_Unadj_Split As Decimal = Pri_Unadj_Split + KS3_Unadj_Split + KS4_Unadj_Split
        Dim Diff As Decimal = Total_Unadj_Split - AP_Places_Total
        If KS3_Unadj_Split = 0 And KS4_Unadj_Split = 0 And (Diff > 0 Or Diff < 0) Then
            Result = Pri_Unadj_Split - Diff
        Else
            Result = Pri_Unadj_Split
        End If

        Return result
    End Function

    <Calculation(Id:="f5baca5ede8e483099d244b17372966b")>
    Public Function NOR_P16_HND_HNP_AP_KS3 As Decimal
        Dim result = Decimal.Zero
        Dim AP_Places_Total As Decimal = Datasets.HighNeedsPlaces.Totalpre16APplaces
        Dim NOR_P02_PRI As Decimal = NOR_P02_PRI
        Dim NOR_P07_KS3 As Decimal = NOR_P07_KS3
        Dim NOR_P08_KS4 As Decimal = NOR_P08_KS4
        Dim F100_AllAcademies As Decimal = F100_AllAcademies
        Dim Pri_Unadj_Split As Decimal = math.round(AP_Places_Total * NOR_P02_PRI / (NOR_P02_PRI + NOR_P07_KS3 + NOR_P08_KS4), 0)
        Dim KS3_Unadj_Split As Decimal = math.round(AP_Places_Total * NOR_P07_KS3 / (NOR_P02_PRI + NOR_P07_KS3 + NOR_P08_KS4), 0)
        Dim KS4_Unadj_Split As Decimal = math.round(AP_Places_Total * NOR_P08_KS4 / (NOR_P02_PRI + NOR_P07_KS3 + NOR_P08_KS4), 0)
        Dim Total_Unadj_Split As Decimal = Pri_Unadj_Split + KS3_Unadj_Split + KS4_Unadj_Split
        Dim Diff As Decimal = Total_Unadj_Split - AP_Places_Total
        If KS3_Unadj_Split > 0 And KS4_Unadj_Split = 0 And (Diff < 0 Or Diff > 0) Then
            Result = KS3_Unadj_Split - Diff
        Else
            Result = KS3_Unadj_Split
        End If

        Return result
    End Function

    <Calculation(Id:="ed13b225693e44b5926bbaf3c8c3d1b7")>
    Public Function NOR_P17_HND_HNP_AP_KS4 As Decimal
        Dim result = Decimal.Zero
        Dim AP_Places_Total As Decimal = Datasets.HighNeedsPlaces.Totalpre16APplaces
        Dim NOR_P02_PRI As Decimal = NOR_P02_PRI
        Dim NOR_P07_KS3 As Decimal = NOR_P07_KS3
        Dim NOR_P08_KS4 As Decimal = NOR_P08_KS4
        Dim F100_AllAcademies As Decimal = F100_AllAcademies
        Dim Pri_Unadj_Split As Decimal = math.round(AP_Places_Total * NOR_P02_PRI / (NOR_P02_PRI + NOR_P07_KS3 + NOR_P08_KS4), 0)
        Dim KS3_Unadj_Split As Decimal = math.round(AP_Places_Total * NOR_P07_KS3 / (NOR_P02_PRI + NOR_P07_KS3 + NOR_P08_KS4), 0)
        Dim KS4_Unadj_Split As Decimal = math.round(AP_Places_Total * NOR_P08_KS4 / (NOR_P02_PRI + NOR_P07_KS3 + NOR_P08_KS4), 0)
        Dim Total_Unadj_Split As Decimal = Pri_Unadj_Split + KS3_Unadj_Split + KS4_Unadj_Split
        Dim Diff As Decimal = Total_Unadj_Split - AP_Places_Total
        If KS4_Unadj_Split > 0 And (Diff > 0 Or Diff < 0) Then
            Result = KS4_Unadj_Split - Diff
        Else
            Result = KS4_Unadj_Split
        End If

        Return result
    End Function

    <Calculation(Id:="b73f801f1d974edf994baaa3d4729853")>
    Public Function NOR_P18_HND_HN_Pre16 As Decimal
        Dim result = Decimal.Zero
        Dim F100_AllAcademies As Decimal = F100_AllAcademies
        Dim Total_HNs_Places_Pre16 As Decimal = Datasets.HighNeedsPlaces.Totalpre16HNsplaces
        If currentscenario.periodid = 2017181 And (F100_AllAcademies = 17181 Or F100_AllAcademies = 17182 Or F100_AllAcademies = 17183) Then
            Result = Total_HNs_Places_Pre16
        Else
            Exclude(rid)
        End if

        Return result
    End Function

    <Calculation(Id:="e827ee0fce3740049ea1a21ff559ebf4")>
    Public Function NOR_P19_HND_AP_Pre16 As Decimal
        Dim result = Decimal.Zero
        Dim F100_AllAcademies = F100_AllAcademies
        Dim Total_AP_Places_Pre16 As Decimal = Datasets.HighNeedsPlaces.Totalpre16APplaces
        If currentscenario.periodid = 2017181 And (F100_AllAcademies = 17181 Or F100_AllAcademies = 17182 Or F100_AllAcademies = 17183) Then
            Result = Total_AP_Places_Pre16
        Else
            Exclude(rid)
        End If

        Return result
    End Function

    <Calculation(Id:="6338ff5ac268480caa3fd9916a568b7b")>
    Public Function NOR_P20_HND_Hosp_Pl As Decimal
        Dim result = Decimal.Zero
        Dim F100_AllAcademies = F100_AllAcademies
        Dim NOR_HND_Hosp_Pl As Decimal = Datasets.HighNeedsPlaces.Hospitalprovisionplaces
        If currentscenario.periodid = 2017181 And (F100_AllAcademies = 17181 Or F100_AllAcademies = 17182 Or F100_AllAcademies = 17183) then
            Result = NOR_HND_Hosp_Pl
        Else
            Exclude(rid)
        End If

        Return result
    End Function

    <Calculation(Id:="275efe39668748899b247bac88876ea9")>
    Public Function NOR_P21_P16 As Decimal
        Dim result = Decimal.Zero
        Dim NOR_P21_P16 As Decimal = Products.AY1718_Acad_Post16.P03_Learners
        Dim F100_AllAcademies As Decimal = F100_AllAcademies
        If currentscenario.periodid = 2017181 And (F100_AllAcademies = 17181 Or F100_AllAcademies = 17182 Or F100_AllAcademies = 17183) then
            Result = NOR_P21_P16
        Else
            Exclude(rid)
        End If

        Return result
    End Function

    <Calculation(Id:="5d8eea88210b42c387d0ea82f084e732")>
    Public Function NOR_P21b_P16_HN As Decimal
        Dim result = Decimal.Zero
        Dim NOR_P21_P16 As Decimal = Products.AY1718_Acad_Post16.P04_HNPlaces
        Dim F100_AllAcademies As Decimal = F100_AllAcademies
        If currentscenario.periodid = 2017181 And (F100_AllAcademies = 17181 Or F100_AllAcademies = 17182 Or F100_AllAcademies = 17183) then
            Result = NOR_P21_P16
        Else
            Exclude(rid)
        End If

        Return result
    End Function

    <Calculation(Id:="1b97b37ba4f44b0d8cf9dd68be2113a9")>
    Public Function AY1718_FundingBasis As Decimal
        Dim result = Decimal.Zero
        Dim AcadFilter As Decimal = F100_AllAcademies
        Dim FundingBasis As Decimal = F900_FundingBasis
        If currentscenario.PeriodID = 2017181 And (AcadFilter = 17181 Or AcadFilter = 17182 Or AcadFilter = 17183) And FundingBasis > 0 Then
            Result = FundingBasis
        Else
            exclude(rid)
        End If

        Return result
    End Function

    <Calculation(Id:="afbd3b455d254f998902de8335b66ad9")>
    Public Function NOR_P42a_Year_Groups_Primary As Decimal
        Dim result = Decimal.Zero
        Dim IsNull As Boolean = iif(Datasets.APTInputsandAdjustments.NumberofPrimaryyeargroupsforallschools, false, true)
        Dim F100_AllAcademies As Decimal = F100_AllAcademies
        Dim F900_FundingBasis As Decimal = F900_FundingBasis
        Dim Primary_YP_InAdj As Decimal = Datasets.APTInputsandAdjustments.NumberofPrimaryyeargroupsforallschools
        Dim Primary_YG_Census As Decimal = Datasets.CensusPupilCharacteristics.NumberofPrimaryyeargroupsforallschools
        Dim Primary_Middle_YG_Census As Decimal = Datasets.CensusPupilCharacteristics.NumberofPrimaryyeargroupsformiddleschools
        Dim Primary_YG_Estimate As Decimal = P010_NOR_TotalPri_YG
        Dim Primary_Year_Groups As Decimal
        Print(F900_FundingBasis, "F900_FundingBasis", rid)
        Print(Primary_YG_Census, "Primary_YG_Census", rid)
        Print(Primary_Middle_YG_Census, "Primary_Middle_YG_Census", rid)
        Print(Primary_YG_Estimate, "Primary_YG_Estimate", rid)
        If currentscenario.periodid = 2017181 And (F100_AllAcademies = 17181 Or F100_AllAcademies = 17182 Or F100_AllAcademies = 17183) Then
            If F900_FundingBasis = 1 And IsNull = False Then
                Primary_Year_Groups = Primary_YP_InAdj
            Else If F900_FundingBasis = 1 And IsNull = true Then
                Primary_Year_Groups = Primary_YG_Census
            Else If F900_FundingBasis = 2
                Primary_Year_Groups = Primary_YG_Estimate
            Else
                Primary_Year_Groups = 0
            End If
        Else
            Exclude(rid)
        End If

        result = Primary_Year_Groups
        Return result
    End Function

    <Calculation(Id:="c231a378e57c47b883b7f20c29910407")>
    Public Function NOR_P42b_Year_Groups_Secondary As Decimal
        Dim result = Decimal.Zero
        Dim IsNull = iif(Datasets.APTInputsandAdjustments.NumberofSecondaryyeargroupsforallschools, false, true)
        Dim F100_AllAcademies As Decimal = F100_AllAcademies
        Dim F900_FundingBasis = F900_FundingBasis
        Dim Sec_YG_InAdj = Datasets.APTInputsandAdjustments.NumberofSecondaryyeargroupsforallschools
        Dim Sec_YG_Census = Datasets.CensusPupilCharacteristics.NumberofSecondaryyeargroupsforallschools
        Dim Sec_Middle_YG_Census = Datasets.CensusPupilCharacteristics.NumberofSecondaryyeargroupsformiddleschools
        Dim Sec_YG_Estimate = P011_NOR_TotalSec_YG
        Dim Secondary_Year_Groups As Decimal
        Print(F900_FundingBasis, "F900_FundingBasis", rid)
        Print(Sec_YG_Census, "Sec_YG_Census", rid)
        Print(Sec_Middle_YG_Census, "Sec_Middle_YG_Census", rid)
        Print(Sec_YG_Estimate, "Sec_YG_Estimate", rid)
        If currentscenario.periodid = 2017181 And (F100_AllAcademies = 17181 Or F100_AllAcademies = 17182 Or F100_AllAcademies = 17183) Then
            If F900_FundingBasis = 1 And IsNull = False Then
                Secondary_Year_Groups = Sec_YG_InAdj
            ElseIf F900_FundingBasis = 1 And IsNull = true Then
                Secondary_Year_Groups = Sec_YG_Census
            ElseIf F900_FundingBasis = 2 Then
                Secondary_Year_Groups = Sec_YG_Estimate
            Else
                Secondary_Year_Groups = 0
            End If
        Else
            Exclude(rid)
        End If

        result = Secondary_Year_Groups
        Return result
    End Function

    <Calculation(Id:="fdc0b649f1bb4e61991a957b82b964db")>
    Public Function NOR_P08a_Y7 As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim IandANOR_Y7 As Decimal = Datasets.APTInputsandAdjustments.NORY7
        Dim CNCNOR_Y7 As Decimal = Datasets.CensusNumberCounts.NORY7
        Dim ENCNOR_Y7 As Decimal = Datasets.EstimateNumberCounts.EstNORY7
        Dim AcadFilter As Decimal = F100_AllAcademies
        Dim IandAPupilNumbers As String = Datasets.APTInputsandAdjustments.Amendedpupilnumbersguaranteed ? 
        Dim IandANOR As Decimal = Datasets.APTInputsandAdjustments.NOR
        Dim FundingBasis As Decimal = F900_FundingBasis
        Dim IsNull As Boolean = iif(Datasets.APTInputsandAdjustments.NORKS3, false, true)
        IF currentscenario.periodid = 2017181 AND (AcadFilter = 17181 OR AcadFilter = 17182 OR AcadFilter = 17183) THEN
            IF FundingBasis = 1 AND ISNULL = False THEN
                Result = IandANOR_Y7
            ELSEIF FundingBasis = 1 AND ISNULL = True THEN
                Result = CNCNOR_Y7
            ELSEIF FundingBasis = 2 AND IandAPupilNumbers = "YES" AND (IandANOR > ENCNOR_Y7) THEN
                Result = IandANOR_Y7
            ELSEIF FundingBasis = 2 THEN
                Result = ENCNOR_Y7
            End if
        Else
            Exclude(rid)
        End if

        Return result
    End Function

    <Calculation(Id:="60ba9a1c98454061a99a345713560661")>
    Public Function NOR_P08b_Y8to11 As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim IandANOR_Y8to11 As Decimal = Datasets.APTInputsandAdjustments.NORY811
        Dim CNCNOR_Y8to11 As Decimal = Datasets.CensusNumberCounts.NORY811
        Dim ENCNOR_Y8 As Decimal = Datasets.EstimateNumberCounts.EstNORY8
        Dim ENCNOR_Y9 As Decimal = Datasets.EstimateNumberCounts.EstNORY9
        Dim ENCNOR_Y10 As Decimal = Datasets.EstimateNumberCounts.EstNORY10
        Dim ENCNOR_Y11 As Decimal = Datasets.EstimateNumberCounts.EstNORY11
        Dim RH As Decimal = P005_NOR_Est_Y8to11
        Dim ENCNOR_Y8to11 As Decimal =(ENCNOR_Y8 + ENCNOR_Y9 + ENCNOR_Y10 + ENCNOR_Y11)
        Dim AcadFilter As Decimal = F100_AllAcademies
        Dim IandAPupilNumbers As String = Datasets.APTInputsandAdjustments.Amendedpupilnumbersguaranteed ? 
        Dim IandANOR As Decimal = Datasets.APTInputsandAdjustments.NOR
        Dim FundingBasis As Decimal = F900_FundingBasis
        Dim IsNull As Boolean = iif(Datasets.APTInputsandAdjustments.NORY811, false, true)
        print(CNCNOR_Y8to11, "Data", rid)
        print(fundingBasis, "FundingBasis", rid)
        print(IsNull, "IsNull", rid)
        IF currentscenario.periodid = 2017181 AND (AcadFilter = 17181 OR AcadFilter = 17182 OR AcadFilter = 17183) THEN
            IF FundingBasis = 1 AND ISNULL = False THEN
                Result = IandANOR_Y8to11
            ELSEIF FundingBasis = 1 AND ISNULL = True THEN
                Result = CNCNOR_Y8to11
            ELSEIF FundingBasis = 2 AND IandAPupilNumbers = "YES" AND (IandANOR > ENCNOR_Y8to11) THEN
                Result = IandANOR_Y8to11
            ELSEIF FundingBasis = 2 THEN
                Result = ENCNOR_Y8to11
            End if
        Else
            Exclude(rid)
        End if

        Return result
    End Function

    <Calculation(Id:="d3e492c246394f94a8e1aeed8248c742")>
    Public Function P001_NOR_Est_Pri As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim Reception As Decimal = Datasets.EstimateNumberCounts.EstNORReception
        Dim Y1 As Decimal = Datasets.EstimateNumberCounts.EstNORY1
        Dim Y2 As Decimal = Datasets.EstimateNumberCounts.EstNORY2
        Dim Y3 As Decimal = Datasets.EstimateNumberCounts.EstNORY3
        Dim Y4 As Decimal = Datasets.EstimateNumberCounts.EstNORY4
        Dim Y5 As Decimal = Datasets.EstimateNumberCounts.EstNORY5
        Dim Y6 As Decimal = Datasets.EstimateNumberCounts.EstNORY6
        Print(Reception, "Reception", rid)
        Print(Y1, "Year 1", rid)
        Print(Y2, "Year 2", rid)
        Print(Y3, "Year 3", rid)
        Print(Y4, "Year 4", rid)
        Print(Y5, "Year 5", rid)
        Print(Y6, "Year 6", rid)
        result =(Reception + Y1 + Y2 + Y3 + Y4 + Y5 + Y6)
        Return result
    End Function

    <Calculation(Id:="b46ba673d6a14b659c0899aec3cf4c35")>
    Public Function P002_NOR_Est_Y1to4 As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim Y1 As Decimal = Datasets.EstimateNumberCounts.EstNORY1
        Dim Y2 As Decimal = Datasets.EstimateNumberCounts.EstNORY2
        Dim Y3 As Decimal = Datasets.EstimateNumberCounts.EstNORY3
        Dim Y4 As Decimal = Datasets.EstimateNumberCounts.EstNORY4
        result =(Y1 + Y2 + Y3 + Y4)
        Return result
    End Function

    <Calculation(Id:="3a11d9b73a0e43768b0b67460febf644")>
    Public Function P003_NOR_Est_Y5to6 As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim Y5 As Decimal = Datasets.EstimateNumberCounts.EstNORY5
        Dim Y6 As Decimal = Datasets.EstimateNumberCounts.EstNORY6
        Result = Y5 + Y6
        Return result
    End Function

    <Calculation(Id:="36b32bfef441490da07887fe87fd3d32")>
    Public Function P004_NOR_Est_Sec As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim Y7 As Decimal = Datasets.EstimateNumberCounts.EstNORY7
        Dim Y8 As Decimal = Datasets.EstimateNumberCounts.EstNORY8
        Dim Y9 As Decimal = Datasets.EstimateNumberCounts.EstNORY9
        Dim Y10 As Decimal = Datasets.EstimateNumberCounts.EstNORY10
        Dim Y11 As Decimal = Datasets.EstimateNumberCounts.EstNORY11
        result =(Y7 + Y8 + Y9 + Y10 + Y11)
        Return result
    End Function

    <Calculation(Id:="598e6685df064b44b654f85ba4e5eb68")>
    Public Function P005_NOR_Est_Y8to11 As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim Y8 As Decimal = Datasets.EstimateNumberCounts.EstNORY8
        Dim Y9 As Decimal = Datasets.EstimateNumberCounts.EstNORY9
        Dim Y10 As Decimal = Datasets.EstimateNumberCounts.EstNORY10
        Dim Y11 As Decimal = Datasets.EstimateNumberCounts.EstNORY11
        result =(Y8 + Y9 + Y10 + Y11)
        Return result
    End Function

    <Calculation(Id:="efde086ab1e14254afc815b2ae7dc541")>
    Public Function P006_NOR_Est_KS3 As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim Y7 As Decimal = Datasets.EstimateNumberCounts.EstNORY7
        Dim Y8 As Decimal = Datasets.EstimateNumberCounts.EstNORY8
        Dim Y9 As Decimal = Datasets.EstimateNumberCounts.EstNORY9
        result =(Y7 + Y8 + Y9)
        Return result
    End Function

    <Calculation(Id:="2c6dd629902447c986f9fe70913ad821")>
    Public Function P007_NOR_Est_KS4 As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim Y10 As Decimal = Datasets.EstimateNumberCounts.EstNORY10
        Dim Y11 As Decimal = Datasets.EstimateNumberCounts.EstNORY11
        result = Y10 + Y11
        Return result
    End Function

    <Calculation(Id:="ead8aadf416a4fec8a5e7e25bae0ee37")>
    Public Function P008_NOR_Est_RtoY11 As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim Pri As Decimal = P001_NOR_Est_Pri
        Dim Sec As Decimal = P004_NOR_Est_Sec
        result = Pri + Sec
        Return result
    End Function

    <Calculation(Id:="32ced350872c49a49b7a9c8409f667d7")>
    Public Function P009_NOR_Est_Y12toY14 As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim Y12 As Decimal = Datasets.EstimateNumberCounts.EstNORY12
        Dim Y13 As Decimal = Datasets.EstimateNumberCounts.EstNORY13
        Dim Y14 As Decimal = Datasets.EstimateNumberCounts.EstNORY14
        result = Y12 + Y13 + Y14
        Return result
    End Function

    <Calculation(Id:="4d77cadefbc1498ab8991cf637f9e7f3")>
    Public Function P010_NOR_TotalPri_YG As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim Reception As Decimal = Datasets.EstimateNumberCounts.EstNORReception
        Dim Y1 As Decimal = Datasets.EstimateNumberCounts.EstNORY1
        Dim Y2 As Decimal = Datasets.EstimateNumberCounts.EstNORY2
        Dim Y3 As Decimal = Datasets.EstimateNumberCounts.EstNORY3
        Dim Y4 As Decimal = Datasets.EstimateNumberCounts.EstNORY4
        Dim Y5 As Decimal = Datasets.EstimateNumberCounts.EstNORY5
        Dim Y6 As Decimal = Datasets.EstimateNumberCounts.EstNORY6
        Dim Reception_YG As Decimal
        Dim Y1_YG As Decimal
        Dim Y2_YG As Decimal
        Dim Y3_YG As Decimal
        Dim Y4_YG As Decimal
        Dim Y5_YG As Decimal
        Dim Y6_YG As Decimal
        If(Reception > 0) then
            Reception_YG = 1
        Else
            Reception_YG = 0
        End if

        If(Y1 > 0) then
            Y1_YG = 1
        Else
            Y1_YG = 0
        End if

        If(Y2 > 0) Then
            Y2_YG = 1
        Else
            Y2_YG = 0
        End if

        If(Y3 > 0) Then
            Y3_YG = 1
        Else
            Y3_YG = 0
        End if

        If(Y4 > 0) Then
            Y4_YG = 1
        Else
            Y4_YG = 0
        End if

        If(Y5 > 0) Then
            Y5_YG = 1
        Else
            Y5_YG = 0
        End if

        If(Y6 > 0) Then
            Y6_YG = 1
        Else
            Y6_YG = 0
        End if

        Result =(Reception_YG + Y1_YG + Y2_YG + Y3_YG + Y4_YG + Y5_YG + Y6_YG)
        Return result
    End Function

    <Calculation(Id:="a709b44507d440b9a9294bd7b715472f")>
    Public Function P011_NOR_TotalSec_YG As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim Y7 As Decimal = Datasets.EstimateNumberCounts.EstNORY7
        Dim Y8 As Decimal = Datasets.EstimateNumberCounts.EstNORY8
        Dim Y9 As Decimal = Datasets.EstimateNumberCounts.EstNORY9
        Dim Y10 As Decimal = Datasets.EstimateNumberCounts.EstNORY10
        Dim Y11 As Decimal = Datasets.EstimateNumberCounts.EstNORY11
        Dim Y7_YG As Decimal
        Dim Y8_YG As Decimal
        Dim Y9_YG As Decimal
        Dim Y10_YG As Decimal
        Dim Y11_YG As Decimal
        If(Y7 > 0) Then
            Y7_YG = 1
        Else
            Y7_YG = 0
        End if

        If(Y8 > 0) Then
            Y8_YG = 1
        Else
            Y8_YG = 0
        End if

        If(Y9 > 0) Then
            Y9_YG = 1
        Else
            Y9_YG = 0
        End if

        If(Y10 > 0) Then
            Y10_YG = 1
        Else
            Y10_YG = 0
        End if

        If(Y11 > 0) Then
            Y11_YG = 1
        Else
            Y11_YG = 0
        End if

        Result =(Y7_YG + Y8_YG + Y9_YG + Y10_YG + Y11_YG)
        Return result
    End Function

    <Calculation(Id:="faa37f236705484a9c066124b771f941")>
    Public Function P012_NOR_Total_YG_R_Y11 As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim P010 As Decimal = P010_NOR_TotalPri_YG
        Dim P011 As Decimal = P011_NOR_TotalSec_YG
        result = P010 + P011
        Return result
    End Function

    <Calculation(Id:="3863b89a5a0541709fc5e88251537a19")>
    Public Function NOR_P23_Total_NOR_KS3_SBS As Decimal
        Dim result = Decimal.Zero
        Dim F100_AllAcademies As Decimal = F100_AllAcademies
        Dim NOR_P07_KS3 As Decimal = NOR_P07_KS3
        Dim F900_FundingBasis As Decimal = F900_FundingBasis
        Dim NOR_P23b_Actual_HN_KS3_Deducted As Decimal = NOR_P23b_Actual_HN_KS3_Deducted
        IF currentscenario.periodID = 2017181 AND (F100_AllAcademies = 17182 OR F100_AllAcademies = 17183) AND F900_FundingBasis = 1 THEN
            Result = NOR_P07_KS3
        ELSE
            Result = NOR_P07_KS3 - NOR_P23b_Actual_HN_KS3_Deducted
        End If

        Print(F900_FundingBasis, "Funding_Basis", rid)
        Print(NOR_P07_KS3, "NOR_P07_KS3", rid)
        Print(NOR_P23b_Actual_HN_KS3_Deducted, "NOR_P23b_Actual_HN_KS3_Deducted", rid)
        Return Result
    End Function

    <Calculation(Id:="dcdd24caf71345cb84362183ec2a4b06")>
    Public Function NOR_P23b_Actual_HN_KS3_Deducted As Decimal
        Dim result = Decimal.Zero
        Dim F100_AllAcademies As Decimal = F100_AllAcademies
        Dim TotalPlacesAPT As Decimal = Datasets.APTLocalfactorsdataset.TotalnumberofHighNeedsplacesin201617
        Dim APT As Boolean
        APT = IIf(Datasets.APTLocalfactorsdataset.TotalnumberofHighNeedsplacesin201617, false, true)
        Dim NOR_Inp_Adj As Decimal = Datasets.APTInputsandAdjustments.NORKS3
        Dim Pre16HNData As Decimal = Datasets.HighNeedsPlaces.Totalpre16HNsplaces
        Dim Pre16APData As Decimal = Datasets.HighNeedsPlaces.Totalpre16APplaces
        Dim TotalPlacesHNData As Decimal = Pre16HNData + Pre16APData
        Dim NOR_P10_APT_HN_KS3 As Decimal = NOR_P10_APT_HN_KS3
        Dim NOR_P16_HND_HNP_AP_KS3 As Decimal = NOR_P16_HND_HNP_AP_KS3
        Dim NOR_P13_HND_HNP_HN_KS3 As Decimal = NOR_P13_HND_HNP_HN_KS3
        Dim HND_HN_KS3 As Decimal = NOR_P16_HND_HNP_AP_KS3 + NOR_P13_HND_HNP_HN_KS3
        Dim F900_FundingBasis As Decimal = F900_FundingBasis
        Dim HN_to_Deduct As Decimal
        If currentscenario.periodid = 2017181 And (F100_AllAcademies = 17181 Or F100_AllAcademies = 17182 Or F100_AllAcademies = 17183) then
            If(APT = True Or TotalPlacesAPT = 0) And TotalPlacesHNData > 0 Then
                HN_to_deduct = 0
            Else If TotalPlacesAPT > TotalPlacesHNData Then
                HN_to_deduct = HND_HN_KS3
            Else
                HN_to_deduct = NOR_P10_APT_HN_KS3
            End If
        Else
            Exclude(rid)
        End If

        Print(F900_FundingBasis, "Funding_Basis", rid)
        Print(NOR_P10_APT_HN_KS3, "NOR_P10_APT_HN_KS3", rid)
        Print(NOR_P16_HND_HNP_AP_KS3, "NOR_P16_HND_HNP_AP_KS3", rid)
        Print(NOR_P13_HND_HNP_HN_KS3, "NOR_P13_HND_HNP_HN_KS3", rid)
        Print(HND_HN_KS3, "HND_HN_KS3", rid)
        Print(HN_to_deduct, "HN_to_deduct", rid)
        Print(APT, "APT", rid)
        If currentscenario.periodid = 2017181 And (F100_AllAcademies = 17182 Or F100_AllAcademies = 17183) And F900_FundingBasis = 1 Then
            Result = 0
        Else
            Result = HN_to_deduct
        End If

        Return Result
    End Function

    <Calculation(Id:="44cff3f96fe24aaeb8b08340f17b7d0c")>
    Public Function NOR_P24_Total_NOR_KS4_SBS As Decimal
        Dim result = Decimal.Zero
        Dim F100_AllAcademies As Decimal = F100_AllAcademies
        Dim NOR_P08_KS4 As Decimal = NOR_P08_KS4
        Dim NOR_P24b_Actual_HN_KS4_Deducted As Decimal = NOR_P24b_Actual_HN_KS4_Deducted
        Dim F900_FundingBasis As Decimal = F900_FundingBasis
        If currentscenario.periodid = 2017181 And (F100_AllAcademies = 17182 Or F100_AllAcademies = 17183) And F900_FundingBasis = 1 Then
            Result = NOR_P08_KS4
        Else
            Result = NOR_P08_KS4 - NOR_P24b_Actual_HN_KS4_Deducted
        End If

        Return result
    End Function

    <Calculation(Id:="1010cec5746f44208af3ef282096232d")>
    Public Function NOR_P24b_Actual_HN_KS4_Deducted As Decimal
        Dim result = Decimal.Zero
        Dim F100_AllAcademies As Decimal = F100_AllAcademies
        Dim TotalPlacesAPT As Decimal = Datasets.APTLocalfactorsdataset.TotalnumberofHighNeedsplacesin201617
        Dim APT As Boolean
        APT = IIf(Datasets.APTLocalfactorsdataset.TotalnumberofHighNeedsplacesin201617, false, true)
        Dim NOR_Inp_Adj As Decimal = Datasets.APTInputsandAdjustments.NORKS4
        Dim Pre16HNData As Decimal = Datasets.HighNeedsPlaces.Totalpre16HNsplaces
        Dim Pre16APData As Decimal = Datasets.HighNeedsPlaces.Totalpre16APplaces
        Dim TotalPlacesHNData As Decimal = Pre16HNData + Pre16APData
        Dim NOR_P08_KS4 As Decimal = NOR_P08_KS4
        Dim NOR_P11_APT_HN_KS4 As Decimal = NOR_P11_APT_HN_KS4
        Dim NOR_P17_HND_HNP_AP_KS4 As Decimal = NOR_P17_HND_HNP_AP_KS4
        Dim NOR_P14_HND_HNP_HN_KS4 As Decimal = NOR_P14_HND_HNP_HN_KS4
        Dim HND_HN_KS4 As Decimal = [NOR_P14_HND_HNP_HN_KS4] + [NOR_P17_HND_HNP_AP_KS4]
        Dim F900_FundingBasis As Decimal = F900_FundingBasis
        Dim HN_to_Deduct As Decimal
        If currentscenario.periodid = 2017181 And (F100_AllAcademies = 17181 Or F100_AllAcademies = 17182 Or F100_AllAcademies = 17183) Then
            If(APT = True Or TotalPlacesAPT = 0) And TotalPlacesHNData > 0 Then
                HN_to_deduct = 0
            ElseIf TotalPlacesAPT > TotalPlacesHNData Then
                HN_to_deduct = NOR_P14_HND_HNP_HN_KS4
            Else
                HN_to_deduct = NOR_P11_APT_HN_KS4
            End If
        Else
            Exclude(rid)
        End If

        Print(F900_FundingBasis, "F900_FundingBasis", rid)
        Print(NOR_P08_KS4, "NOR_P08_KS4", rid)
        Print(NOR_P11_APT_HN_KS4, "NOR_P11_APT_HN_KS4", rid)
        Print(NOR_P17_HND_HNP_AP_KS4, "NOR_P17_HND_HNP_AP_KS4", rid)
        Print(NOR_P14_HND_HNP_HN_KS4, "NOR_P14_HND_HNP_HN_KS4", rid)
        Print(HND_HN_KS4, "HND_HN_KS4", rid)
        Print(HN_to_deduct, "HN_to_deduct", rid)
        Print(APT, "APT", rid)
        If currentscenario.periodid = 2017181 And (F100_AllAcademies = 17182 Or F100_AllAcademies = 17183) And F900_FundingBasis = 1 Then
            Result = 0
        Else
            Result = HN_to_deduct
        End If

        Return result
    End Function

    <Calculation(Id:="f242ac7024bd4a3898c4d07f18326c8c")>
    Public Function NOR_P25_Total_NOR_SEC_SBS As Decimal
        Dim result = Decimal.Zero
        Dim F100_AllAcademies As Decimal = F100_AllAcademies
        Dim P23_Total_NOR_KS3_SBS As Decimal = NOR_P23_Total_NOR_KS3_SBS
        Dim P24_Total_NOR_KS4_SBS As Decimal = NOR_P24_Total_NOR_KS4_SBS
        Print(P23_Total_NOR_KS3_SBS, "P23_Total_NOR_KS3_SBS", rid)
        Print(P24_Total_NOR_KS4_SBS, "P24_Total_NOR_KS4_SBS", rid)
        If currentscenario.periodid = 2017181 And (F100_AllAcademies = 17181 Or F100_AllAcademies = 17182 Or F100_AllAcademies = 17183) then
            Result = P23_Total_NOR_KS3_SBS + P24_Total_NOR_KS4_SBS
        Else
            Exclude(rid)
        End If

        Return result
    End Function

    <Calculation(Id:="994afe64060c4c6b9e3d30000d67e930")>
    Public Function NOR_P25b_Actual_HN_Sec_deducted As Decimal
        Dim result = Decimal.Zero
        Dim F100_AllAcademies = F100_AllAcademies
        Dim P23b_Actual_HN_KS3_Deduct As Decimal = NOR_P23b_Actual_HN_KS3_Deducted
        Dim P24b_Actual_HN_KS4_Deduct As Decimal = NOR_P24b_Actual_HN_KS4_deducted
        Print(P23b_Actual_HN_KS3_Deduct, "P23b_Actual_HN_KS3_Deduct", rid)
        Print(P24b_Actual_HN_KS4_Deduct, "P24b_Actual_HN_KS4_Deduct", rid)
        If currentscenario.periodid = 2017181 And (F100_AllAcademies = 17181 Or F100_AllAcademies = 17182 Or F100_AllAcademies = 17183) Then
            Result = P23b_Actual_HN_KS3_Deduct + P24b_Actual_HN_KS4_Deduct
        Else
            Exclude(rid)
        End If

        Return result
    End Function

    <Calculation(Id:="513a3129b0b4408ea81e014cd4212fe1")>
    Public Function NOR_P26_Total_NOR_SBS As Decimal
        Dim result = Decimal.Zero

        Dim P22_Total_NOR_Pri_SBS As Decimal = NOR_P22_Total_NOR_PRI_SBS
        Dim P25_Total_NOR_Sec_SBS As Decimal = NOR_P25_Total_NOR_SEC_SBS
        Dim P26b_Total_Actual_HN_Deducted As Decimal = NOR_P26b_Total_Actual_HN_deducted
        Print(P22_Total_NOR_Pri_SBS, "P22_Total_NOR_Pri_SBS", rid)
        Print(P25_Total_NOR_Sec_SBS, "P25_Total_NOR_Sec_SBS", rid)
        If currentscenario.periodid = 2017181 And F100_AllAcademies = 17181 Then
            Result = P22_Total_NOR_Pri_SBS + P25_Total_NOR_Sec_SBS
        ElseIf currentscenario.periodid = 2017181 And (F100_AllAcademies = 17182 Or F100_AllAcademies = 17183) Then
            Result = P22_Total_NOR_Pri_SBS + P25_Total_NOR_Sec_SBS - P26b_Total_Actual_HN_Deducted
        Else
            Exclude(rid)
        End If

        Return result
    End Function

    <Calculation(Id:="3add8cd82c0640b9ae014e637e4414a7")>
    Public Function NOR_P26b_Total_Actual_HN_deducted As Decimal
        Dim result = Decimal.Zero
        Dim F100_AllAcademies As Decimal = F100_AllAcademies
        Dim P22b_Actual_HN_Pri_Deduct As Decimal = NOR_P22b_Actual_HN_Pri_deducted
        Dim P23b_Actual_HN_KS3_Deduct As Decimal = NOR_P23b_Actual_HN_KS3_deducted
        Dim P24b_Actual_HN_KS4_Deduct As Decimal = NOR_P24b_Actual_HN_KS4_deducted
        Dim APT_Pri As Decimal = Datasets.APTLocalfactorsdataset.NumberofprimarypupilsonrollattheschoolinHighNeedsplacesin201617
        Dim APT_KS3 As Decimal = Datasets.APTLocalfactorsdataset.NumberofKS3pupilsonrollattheschoolinHighNeedsplacesin201617
        Dim APT_KS4 As Decimal = Datasets.APTLocalfactorsdataset.NumberofKS4pupilsonrollattheschoolinHighNeedsplacesin201617
        Print(P22b_Actual_HN_Pri_Deduct, "P22b_Actual_HN_Pri_Deduct", rid)
        Print(P23b_Actual_HN_KS3_Deduct, "P23b_Actual_HN_KS3_Deduct", rid)
        Print(P24b_Actual_HN_KS4_Deduct, "P24b_Actual_HN_KS4_Deduct", rid)
        If currentscenario.periodid = 2017181 And F100_AllAcademies = 17181 Then
            Result = P22b_Actual_HN_Pri_Deduct + P23b_Actual_HN_KS3_Deduct + P24b_Actual_HN_KS4_Deduct
        ElseIf currentscenario.periodid = 2017181 And (F100_AllAcademies = 17182 Or F100_AllAcademies = 17183) Then
            Result = APT_Pri + APT_KS3 + APT_KS4
        Else
            Exclude(rid)
        End If

        Return result
    End Function

    <Calculation(Id:="258d5f933f2a4896bfcb9e3a3986b63b")>
    Public Function NOR_P22_Total_NOR_PRI_SBS As Decimal
        Dim result = Decimal.Zero
        Dim NOR_P02_Pri As Decimal = NOR_P02_PRI
        Dim NOR_P01_RU As Decimal = NOR_P01_RU
        Dim NOR_P22b_Actual_HN_Pri_Deducted As Decimal = NOR_P22b_Actual_HN_Pri_Deducted
        Dim F900_FundingBasis As Decimal = F900_FundingBasis
        Dim F100_AllAcademies As Decimal = F100_AllAcademies
        Print(NOR_P01_RU, "NOR P01 RU", rid)
        Print(NOR_P02_Pri, "NOR P02 Pri", rid)
        Print(NOR_P22b_Actual_HN_Pri_Deducted, "NOR P02 Pri", rid)
        If currentscenario.periodid = 2017181 And (F100_AllAcademies = 17182 Or F100_AllAcademies = 17183) And F900_FundingBasis = 1 Then
            Result = NOR_P02_Pri + NOR_P01_RU
        Else
            Result = NOR_P02_Pri + NOR_P01_RU - NOR_P22b_Actual_HN_Pri_Deducted
        End If

        Return result
    End Function

    <Calculation(Id:="676a94f915064ed4944226446d98d589")>
    Public Function NOR_P22b_Actual_HN_Pri_Deducted As Decimal
        Dim result = Decimal.Zero
        Dim F100_AllAcademies As Decimal = F100_AllAcademies
        Dim TotalPlacesAPT As Decimal = Datasets.APTLocalfactorsdataset.TotalnumberofHighNeedsplacesin201617
        Dim APT As Boolean
        APT = IIf(Datasets.APTLocalfactorsdataset.TotalnumberofHighNeedsplacesin201617, false, true)
        Dim NOR_Inp_Adj As Decimal = Datasets.APTInputsandAdjustments.NORPrimary
        Dim Pre16HNData As Decimal = Datasets.HighNeedsPlaces.Totalpre16HNsplaces
        Dim Pre16APData As Decimal = Datasets.HighNeedsPlaces.Totalpre16APplaces
        Dim TotalPlacesHNData As Decimal = Pre16HNData + Pre16APData
        Dim NOR_P02_Pri As Decimal = NOR_P02_PRI
        Dim NOR_P01_RU As Decimal = NOR_P01_RU
        Dim NOR_P09_APT_HN_Pri As Decimal = NOR_P09_APT_HN_PRI
        Dim NOR_P15_HND_HNP_AP_Pri As Decimal = NOR_P15_HND_HNP_AP_PRI
        Dim NOR_P12_HND_HNP_HN_Pri As Decimal = NOR_P12_HND_HNP_HN_PRI
        Dim HND_HN_Pri As Decimal = NOR_P12_HND_HNP_HN_Pri + NOR_P15_HND_HNP_AP_Pri
        Dim F900_FundingBasis As Decimal = F900_FundingBasis
        Dim HN_to_Deduct As Decimal
        If currentscenario.periodid = 2017181 And (F100_AllAcademies = 17181 Or F100_AllAcademies = 17182 Or F100_AllAcademies = 17183) then
            If(APT = True Or TotalPlacesAPT = 0) And TotalPlacesHNData > 0 Then
                HN_to_deduct = 0
            Else If TotalPlacesAPT > TotalPlacesHNData Then
                HN_to_deduct = HND_HN_Pri '(NOR_P12_HND_HNP_HN_Pri + NOR_P15_HND_HNP_AP_Pri)
            Else
                HN_to_deduct = NOR_P09_APT_HN_Pri
            End If
        Else
            Exclude(rid)
        End If

        Print(TotalPlacesAPT, "Total Places APT", rid)
        Print(TotalPlacesHNData, "Total Places HN Data", rid)
        Print(NOR_P09_APT_HN_Pri, "NOR_P09_APT HN Pri", rid)
        Print(NOR_P15_HND_HNP_AP_Pri, "NOR_P15_HND_HNP_AP_Pri", rid)
        Print(NOR_P12_HND_HNP_HN_Pri, "NOR_P12_HND_HNP_HN_Pri", rid)
        Print(HND_HN_Pri, "HND_HN_Pri", rid)
        Print(HN_to_deduct, "HN_to_deduct", rid)
        Print(APT, "APT", rid)
        Print(F100_AllAcademies, "F100_AllAcademies", rid)
        If currentscenario.periodid = 2017181 And (F100_AllAcademies = 17182 Or F100_AllAcademies = 17183) And F900_FundingBasis = 1 Then
            Result = 0
        Else
            Result = HN_to_deduct
        End If

        Return result
    End Function

    <Calculation(Id:="684215f229604e8ab82d13574f588e65")>
    Public Function NOR_P27a_Total_NOR_MFG As Decimal
        Dim result = Decimal.Zero
        Dim F100_AllAcademies As Decimal = F100_AllAcademies
        Dim NOR_P02_PRI As Decimal = NOR_P02_PRI
        Dim NOR_P06_SEC As Decimal = NOR_P06_SEC
        Print(F100_AllAcademies, "F100_AllAcademies", rid)
        If currentscenario.periodid = 2017181 And (F100_AllAcademies = 17181 Or F100_AllAcademies = 17182 Or F100_AllAcademies = 17183) Then
            Result = NOR_P02_PRI + NOR_P06_SEC
        Else
            Exclude(rid)
        End If

        Return result
    End Function

    <Calculation(Id:="970fbc1809a045778f7308d7728bb3b6")>
    Public Function NOR_P27b_Total_HN_MFG As Decimal
        Dim result = Decimal.Zero
        Dim F100_AllAcademies As Decimal = F100_AllAcademies
        Dim F900_FundingBasis As Decimal = F900_FundingBasis
        Dim P22b_Actual_HN_Pri_deduct As Decimal = NOR_P22b_Actual_HN_Pri_deducted
        Dim P23b_Actual_HN_KS3_deduct As Decimal = NOR_P23b_Actual_HN_KS3_deducted
        Dim P24b_Actual_HN_KS4_deduct As Decimal = NOR_P24b_Actual_HN_KS4_deducted
        Dim APT_Pri As Decimal = Datasets.APTLocalfactorsdataset.NumberofprimarypupilsonrollattheschoolinHighNeedsplacesin201617
        Dim APT_KS3 As Decimal = Datasets.APTLocalfactorsdataset.NumberofKS3pupilsonrollattheschoolinHighNeedsplacesin201617
        Dim APT_KS4 As Decimal = Datasets.APTLocalfactorsdataset.NumberofKS4pupilsonrollattheschoolinHighNeedsplacesin201617
        Print(P22b_Actual_HN_Pri_deduct, "P22b_Actual_HN_Pri_deduct", rid)
        Print(P23b_Actual_HN_KS3_deduct, "P23b_Actual_HN_KS3_deduct", rid)
        Print(P24b_Actual_HN_KS4_deduct, "P24b_Actual_HN_KS4_deduct", rid)
        If currentscenario.periodid = 2017181 And F100_AllAcademies = 17181 Then
            Result = P22b_Actual_HN_Pri_deduct + P23b_Actual_HN_KS3_deduct + P24b_Actual_HN_KS4_deduct
        ElseIf currentscenario.periodid = 2017181 And (F100_AllAcademies = 17182 Or F100_AllAcademies = 17183) Then
            Result = APT_Pri + APT_KS3 + APT_KS4
        Else
            Exclude(rid)
        End If

        Return result
    End Function

    <Calculation(Id:="69f8ec02afb54f4eaaf860dcfae7df64")>
    Public Function NOR_P27c_total_NOR_MFG_forPupilMatrix As Decimal
        Dim result = Decimal.Zero
        Dim F100_AllAcademies As Decimal = F100_AllAcademies
        Dim P22_Total_NOR_Pri_SBS As Decimal = NOR_P22_Total_NOR_PRI_SBS
        Dim P25_Total_NOR_Sec_SBS As Decimal = NOR_P25_Total_NOR_SEC_SBS
        Dim NOR_P01_RU As Decimal = NOR_P01_RU
        Dim NOR_P26b_Total_Actual_HN_Deducted As Decimal = NOR_P26b_Total_Actual_HN_deducted
        Print(P22_Total_NOR_Pri_SBS, "P22_Total_NOR_Pri_SBS", rid)
        Print(P25_Total_NOR_Sec_SBS, "P25_Total_NOR_Sec_SBS", rid)
        Print(NOR_P01_RU, "NOR_P01_RU", rid)
        If currentscenario.periodid = 2017181 And F100_AllAcademies = 17181 Then
            Result = P22_Total_NOR_Pri_SBS - NOR_P01_RU + P25_Total_NOR_Sec_SBS
        ElseIf currentscenario.periodid = 2017181 And (F100_AllAcademies = 17182 Or F100_AllAcademies = 17183) Then
            Result = P22_Total_NOR_Pri_SBS - NOR_P01_RU + P25_Total_NOR_Sec_SBS - NOR_P26b_Total_Actual_HN_Deducted
        Else
            Exclude(rid)
        End If

        Return Result
    End Function

    <Calculation(Id:="f8c77500ab2648a9b468d555325dc38d")>
    Public Function NOR_P28_Total_NOR_Mainstream_ESG As Decimal
        Dim result = Decimal.Zero
        Dim F100_AllAcademies = F100_AllAcademies
        Dim NOR_P05_Nur As Decimal = NOR_P05_NUR
        Dim NOR_P02_Pri As Decimal = NOR_P02_PRI
        Dim NOR_P06_Sec As Decimal = NOR_P06_SEC
        Dim NOR_P21_P16 As Decimal = NOR_P21_P16
        Dim NOR_P01_RU As Decimal = NOR_P01_RU
        Dim NOR_P18_HND_HN_Pre16 As Decimal = NOR_P18_HND_HN_Pre16
        Dim NOR_P19_HND_AP_Pre16 As Decimal = NOR_P19_HND_AP_Pre16
        Dim NOR_P20_HND_Hosp_Plc As Decimal = NOR_P20_HND_Hosp_Pl
        'ESG does Not exist In 1718, so these have been Set To zero
        'if currentscenario.periodid = 2017181 And F100_AllAcademies = 17181 Then    
        '  result = NOR_P05_Nur + NOR_P02_Pri + NOR_P06_Sec + NOR_P21_P16
        ' Else If currentscenario.periodid = 2017181 And (F100_AllAcademies = 17182 Or F100_AllAcademies = 17183) then  
        '  result = NOR_P05_Nur + NOR_P02_Pri - NOR_P01_RU + NOR_P06_Sec + NOR_P21_P16 + NOR_P18_HND_HN_Pre16 + NOR_P19_HND_AP_Pre16 + NOR_P20_HND_Hosp_Plc
        'Else Exclude(rid)
        'End If
        '   Print(NOR_P05_Nur, "NOR_P05_Nur", rid)
        '   Print(NOR_P02_Pri, "NOR_P02_Pri", rid)
        '   Print(NOR_P06_Sec, "NOR_P06_Sec", rid)
        '   Print(NOR_P21_P16, "NOR_P21_P16", rid)
        '   Print(NOR_P01_RU, "NOR_P01_RU", rid)
        result = 0
        Return result
    End Function

    <Calculation(Id:="b6faa21b020d4cd7b6417279f9f424f3")>
    Public Function NOR_P29_Total_NOR_HNPlaces_ESG As Decimal
        Dim result = Decimal.Zero
        Dim F100_AllAcademies As Decimal = F100_AllAcademies
        Dim NOR_P18_HND_HN_Pre16 = NOR_P18_HND_HN_Pre16
        'ESG does Not exist In 1718 so this has been Set To zero
        'If currentscenario.periodid = 2017181 And (F100_AllAcademies = 17181 Or F100_AllAcademies = 17182 Or F100_AllAcademies = 17183) then    
        '  Result = NOR_P18_HND_HN_Pre16
        'Else Exclude(rid)
        'End If
        result = 0
        Return result
    End Function

    <Calculation(Id:="d8f887c22e3e4afe94aea527ad776618")>
    Public Function NOR_P30_Total_NOR_APPlaces_ESG As Decimal
        Dim result = Decimal.Zero
        Dim F100_AllAcademies = F100_AllAcademies
        Dim NOR_P19_HND_AP_Pre16 = NOR_P19_HND_AP_Pre16
        'ESG does Not exist In 1718 so this has been Set To zero
        'If currentscenario.periodid = 2017181 And (F100_AllAcademies = 17181 Or F100_AllAcademies = 17182 Or F100_AllAcademies = 17183) then    
        '   Result = NOR_P19_HND_AP_Pre16
        'Else Exclude(rid)
        'End If
        result = 0
        Return result
    End Function

    <Calculation(Id:="c64f08e8f2bf4805a047fe10fe385147")>
    Public Function NOR_P31_Total_NOR_HospitalPlaces_ESG As Decimal
        Dim result = Decimal.Zero
        Dim F100_AllAcademies As Decimal = F100_AllAcademies
        Dim NOR_P20_HND_Hosp_Pl = NOR_P20_HND_Hosp_Pl
        'ESG does Not exist In 1718 so this has been Set To zero
        'If currentscenario.periodid = 2017181 And (F100_AllAcademies = 17181 Or F100_AllAcademies = 17182 Or F100_AllAcademies = 17183) then    
        '   Result = NOR_P20_HND_Hosp_Pl
        'Else Exclude(rid)
        'End If 
        result = 0
        Return result
    End Function

    <Calculation(Id:="42deae38bb0d4ddfb90c4f76bdf0a62b")>
    Public Function NOR_P33_1617_Base_NOR As Decimal
        Dim result = Decimal.Zero
        Dim F100_AllAcademies As Decimal = F100_AllAcademies
        Dim Scenario_Report_P26 As Decimal = Persisted.Report_AY1617_Acad_NOR : NOR_AY201617_Report.P26_Total_NOR_SBS
        Dim Scenario_Report_P01 As Decimal = Persisted.Report_AY1617_Acad_NOR : NOR_AY201617_Report.P01_NOR_RU
        If currentscenario.periodid = 2017181 And (F100_AllAcademies = 17181 Or F100_AllAcademies = 17182 Or F100_AllAcademies = 17183) then
            result = Scenario_Report_P26 - Scenario_Report_P01
        Else
            Exclude(rid)
        End If

        Return result
    End Function

    <Calculation(Id:="46f4ba7a38b74e11af9971dfea98d519")>
    Public Function NOR_P34_1617_Base_RU As Decimal
        Dim result = Decimal.Zero
        Dim F100_AllAcademies As Decimal = F100_AllAcademies
        Dim Scenario_Report_P01 As Decimal = Persisted.Report_AY1617_Acad_NOR : NOR_AY201617_Report.P01_NOR_RU
        If currentscenario.periodid = 2017181 And (F100_AllAcademies = 17181 Or F100_AllAcademies = 17182 Or F100_AllAcademies = 17183) then
            result = Scenario_Report_P01
        Else
            Exclude(rid)
        End If

        Return result
    End Function

    <Calculation(Id:="695f7fcf052d44799f79edebb128d359")>
    Public Function NOR_P36_1617_pre16_HN As Decimal
        Dim result = Decimal.Zero
        Dim F100_AllAcademies As Decimal = F100_AllAcademies
        Dim Scenario_Report_P18 As Decimal = Persisted.Report_AY1617_Acad_NOR : NOR_AY201617_Report.P18_NOR_HNP_HN_Pre16
        If currentscenario.periodid = 2017181 And (F100_AllAcademies = 17181 Or F100_AllAcademies = 17182 Or F100_AllAcademies = 17183) then
            result = Scenario_Report_P18
        Else
            Exclude(rid)
        End If

        Return result
    End Function

    <Calculation(Id:="e1708739a1424988b835d4efef873eb4")>
    Public Function NOR_P37_1617_pre16_AP As Decimal
        Dim result = Decimal.Zero
        Dim F100_AllAcademies As Decimal = F100_AllAcademies
        Dim Scenario_Report_P19 As Decimal = Persisted.Report_AY1617_Acad_NOR : NOR_AY201617_Report.P19_NOR_HNP_AP_Pre16
        If currentscenario.periodid = 2017181 And (F100_AllAcademies = 17181 Or F100_AllAcademies = 17182 Or F100_AllAcademies = 17183) then
            result = Scenario_Report_P19
        Else
            Exclude(rid)
        End If

        Return result
    End Function

    <Calculation(Id:="883e84ff60f9440c93f930ddf23c79cb")>
    Public Function NOR_P38_1617_HN_Hosp_Pl As Decimal
        Dim result = Decimal.Zero
        Dim F100_AllAcademies As Decimal = F100_AllAcademies
        Dim Scenario_Report_P20 As Decimal = Persisted.Report_AY1617_Acad_NOR : NOR_AY201617_Report.P20_NOR_HNP_Hosp_Pl
        If currentscenario.periodid = 2017181 And (F100_AllAcademies = 17181 Or F100_AllAcademies = 17182 Or F100_AllAcademies = 17183) then
            result = Scenario_Report_P20
        Else
            Exclude(rid)
        End If

        Return result
    End Function

    <Calculation(Id:="a5ca5a16c1f0418d9c89a9cb842bc2e7")>
    Public Function NOR_P39_1617_MFG_NOR As Decimal
        Dim result = Decimal.Zero
        Dim F100_AllAcademies As Decimal = F100_AllAcademies
        Dim Scenario_Report_P35 As Decimal = Persisted.Report_AY1617_Acad_MFG : MFG_AY201617_Report.P35_1617AdjNOR
        Dim Scenario_Report_P60 As Decimal = Persisted.Report_AY1617_Acad_MFG : MFG_AY201617_Report.P60_IY1617AdjNOR
        Dim Scenario_Report_P34 As Decimal = Persisted.Report_AY1617_Acad_MFG : MFG_AY201617_Report.P34_1617EFAPosAdjNOR
        Dim Scenario_Report_P33 As Decimal = Persisted.Report_AY1617_Acad_MFG : MFG_AY201617_Report.P33_1617EFANegAdjNOR
        If currentscenario.periodid = 2017181 And (F100_AllAcademies = 17181 Or F100_AllAcademies = 17182 Or F100_AllAcademies = 17183) then
            Result =(Scenario_Report_P35 + Scenario_Report_P60) - (Scenario_Report_P34 - Scenario_Report_P33)
        Else
            Exclude(rid)
        End If

        Return result
    End Function

    <Calculation(Id:="3b6aa4903a1b44c6bdcb28727e445ba4")>
    Public Function NOR_P40_1617_Post16_NOR As Decimal
        Dim result = Decimal.Zero
        Dim F100_AllAcademies As Decimal = F100_AllAcademies
        Dim Scenario_Report_P03 As Decimal = Persisted.Report_AY1617_Acad_Post16 : Post16_AY201617_Report.P03_Learners
        If currentscenario.periodid = 2017181 And (F100_AllAcademies = 17181 Or F100_AllAcademies = 17182 Or F100_AllAcademies = 17183) then
            result = Scenario_Report_P03
        Else
            Exclude(rid)
        End If

        Return result
    End Function

    <Calculation(Id:="541f72cc910e4b1bb38d7dd2c2a7d9ca")>
    Public Function NOR_P41_1617_Post16_HN As Decimal
        Dim result = Decimal.Zero
        Dim F100_AllAcademies As Decimal = F100_AllAcademies
        Dim Scenario_Report_P04 As Decimal = Persisted.Report_AY1617_Acad_Post16 : Post16_AY201617_Report.P04_HNPlaces
        If currentscenario.periodid = 2017181 And (F100_AllAcademies = 17181 Or F100_AllAcademies = 17182 Or F100_AllAcademies = 17183) then
            result = Scenario_Report_P04
        Else
            Exclude(rid)
        End If

        Return result
    End Function

    <Calculation(Id:="fbe141af54f24467bbec6ba6081b3a8d")>
    Public Function NOR_P51_Total_NOR_Mainstream As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim NOR_P02 As Decimal = NOR_P02_PRI
        Dim NOR_P05 As Decimal = NOR_P05_NUR
        Dim NOR_P06 As Decimal = NOR_P06_SEC
        Dim NOR_P21 As Decimal = NOR_P21_P16
        Dim F100_AllAcademies As Decimal = F100_AllAcademies
        If currentscenario.periodid = 2017181 And F100_AllAcademies = 17181
            result = NOR_P02 + NOR_P05 + NOR_P06 + NOR_P21
        ElseIf currentscenario.periodid = 2017181 And (F100_AllAcademies = 17182 Or F100_AllAcademies = 17183) then
            result = NOR_P02 + NOR_P05 + NOR_P06 + NOR_P21
        Else
            Exclude(rid)
        End If

        Return result
    End Function

    <Calculation(Id:="a0d67eaf492b4972b3cb4d73bc71f7d5")>
    Public Function NOR_P52_Total_NOR_HN_Places As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim NOR_P18 As Decimal = NOR_P18_HND_HN_Pre16
        Dim F100_AllAcademies As Decimal = F100_AllAcademies
        If currentscenario.periodid = 2017181 And (F100_AllAcademies = 17181 Or F100_AllAcademies = 17182 Or F100_AllAcademies = 17183) then
            result = NOR_P18
        Else
            Exclude(rid)
        End If

        Return result
    End Function

    <Calculation(Id:="4c5c489b147a4b28bb047bb7187f6c64")>
    Public Function NOR_P53_Total_NOR_AP_Places As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim NOR_P19 As Decimal = NOR_P19_HND_AP_Pre16
        Dim F100_AllAcademies As Decimal = F100_AllAcademies
        If currentscenario.periodid = 2017181 And (F100_AllAcademies = 17181 Or F100_AllAcademies = 17182 Or F100_AllAcademies = 17183) then
            result = NOR_P19
        Else
            Exclude(rid)
        End If

        Return result
    End Function

    <Calculation(Id:="f69c26075798447a831a89b5ce3da8b2")>
    Public Function NOR_P54_Total_NOR_Hospital_Places As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim NOR_P20 As Decimal = NOR_P20_HND_Hosp_Pl
        Dim F100_AllAcademies As Decimal = F100_AllAcademies
        If currentscenario.periodid = 2017181 And (F100_AllAcademies = 17181 Or F100_AllAcademies = 17182 Or F100_AllAcademies = 17183) then
            result = NOR_P20
        Else
            Exclude(rid)
        End If

        Return result
    End Function

    <Calculation(Id:="d9b7d90fe0ee426a97c766afbb441bee")>
    Public Function NOR_P32_Total_NOR_ESGProt As Decimal
        Dim result = Decimal.Zero
        Dim F100_AllAcademies As Decimal = F100_AllAcademies
        Dim NOR_P01_RU As Decimal = NOR_P01_RU
        Dim P22_Total_NOR_Pri_SBS As Decimal = NOR_P22_Total_NOR_PRI_SBS
        Dim P25_Total_NOR_Sec_SBS As Decimal = NOR_P25_Total_NOR_SEC_SBS
        Dim NOR_P21_P16 As Decimal = NOR_P21_P16
        Dim P26b_Total_Actual_HN_Deducted As Decimal = NOR_P26b_Total_Actual_HN_deducted
        Print(NOR_P01_RU, "NOR_P01_RU", rid)
        Print(P22_Total_NOR_Pri_SBS, "P22_Total_NOR_Pri_SBS", rid)
        Print(P25_Total_NOR_Sec_SBS, "P25_Total_NOR_Sec_SBS", rid)
        Print(NOR_P21_P16, "NOR_P21_P16", rid)
        If currentscenario.periodid = 2017181 Then
            If F100_AllAcademies = 17181 Then
                Result = P22_Total_NOR_Pri_SBS + P25_Total_NOR_Sec_SBS + NOR_P21_P16
            ElseIf(F100_AllAcademies = 17182 Or F100_AllAcademies = 17183) Then
                Result = P22_Total_NOR_Pri_SBS + P25_Total_NOR_Sec_SBS + NOR_P21_P16 - P26b_Total_Actual_HN_Deducted
            End If
        Else
            Exclude(rid)
        End If

        Return result
    End Function

    <Calculation(Id:="817f9f7fca1f4e24acedd353f6a4652f")>
    Public Function NOR_P32b_TOTAL_ESGProt_incHN As Decimal
        Dim result = Decimal.Zero
        Dim F100_AllAcademies As Decimal = F100_AllAcademies
        Dim NOR_P01_RU As Decimal = NOR_P01_RU
        Dim NOR_P02_Pri As Decimal = NOR_P02_PRI
        Dim NOR_P06_Sec As Decimal = NOR_P06_SEC
        Dim NOR_P21_P16 As Decimal = NOR_P21_P16
        Dim NOR_P18_HND_HN_Pre16 As Decimal = NOR_P18_HND_HN_Pre16
        Dim NOR_P20_HND_Hosp_Pl As Decimal = NOR_P20_HND_Hosp_Pl
        Dim NOR_P19_HND_AP_Pre16 As Decimal = NOR_P19_HND_AP_Pre16
        Dim Tot_Places As Decimal = NOR_P18_HND_HN_Pre16 + NOR_P20_HND_Hosp_Pl + NOR_P19_HND_AP_Pre16
        Dim F900_FundingBasis As Decimal = F900_FundingBasis
        Print(Tot_Places, "Tot_Places", rid)
        Print(NOR_P18_HND_HN_Pre16, "NOR_P18_HND_HN_Pre16", rid)
        Print(NOR_P19_HND_AP_Pre16, "NOR_P19_HND_AP_Pre16", rid)
        Print(NOR_P20_HND_Hosp_Pl, "NOR_P20_HND_Hosp_Pl", rid)
        Print(F100_AllAcademies, "F100_AllAcademies", rid)
        Print(F900_FundingBasis, "F900_FundingBasis", rid)
        If currentscenario.periodid = 2017181 And F100_AllAcademies = 17181 And (F900_FundingBasis = 1 Or F900_FundingBasis = 2) Then
            result = NOR_P01_RU + NOR_P02_Pri + NOR_P06_Sec + NOR_P21_P16
        ElseIf currentscenario.periodid = 2017181 And (F100_AllAcademies = 17182 Or F100_AllAcademies = 17183) And (F900_FundingBasis = 1 Or F900_FundingBasis = 2) Then
            result = NOR_P02_Pri + NOR_P06_Sec + NOR_P21_P16
        ElseIf currentscenario.periodid = 2017181 And (F100_AllAcademies = 17181 Or F100_AllAcademies = 17182 Or F100_AllAcademies = 17183) And F900_FundingBasis = 3 Then
            result = NOR_P01_RU + NOR_P02_Pri + NOR_P06_Sec + NOR_P21_P16 + NOR_P18_HND_HN_Pre16 + NOR_P19_HND_AP_Pre16 + NOR_P20_HND_Hosp_Pl
        Else
            Exclude(rid)
        End If

        Return result
    End Function

    <Calculation(Id:="0d25cdeb84184a9dbb451cb81ace0d81")>
    Public Function NOR_P35_1617_BaseNOR_ESGProt As Decimal
        Dim result = Decimal.Zero
        Dim F100_AllAcademies As Decimal = F100_AllAcademies
        Dim Date_Opened As Date = Datasets.ProviderInformation.DateOpened
        Dim NOR_1617_Scenario_Report_P32_Total_NOR_ALP As Decimal = Persisted.Report_AY1617_Acad_NOR : NOR_AY201617_Report.P32_Total_NOR_ALP
        Print(Date_Opened, "Date Opened", rid)
        If currentscenario.periodid = 2017181 And (F100_AllAcademies = 17181 Or F100_AllAcademies = 17182 Or F100_AllAcademies = 17183) then
            result = NOR_1617_Scenario_Report_P32_Total_NOR_ALP
        Else
            Exclude(rid)
        End If

        Return result
    End Function

    <Calculation(Id:="113294a39b2545a6b392f717cba402bb")>
    Public Function NOR_P35b_1617_BaseNOR_ESGProtincHN As Decimal
        Dim result = Decimal.Zero
        Dim F100_AllAcademies As Decimal = F100_AllAcademies
        Dim Date_Opened As Date = Datasets.ProviderInformation.DateOpened
        Dim NOR_1617_Scenario_Report_P32b_Total_NOR_ALPincHN As Decimal = Persisted.Report_AY1617_Acad_NOR : NOR_AY201617_Report.P32b_Total_NOR_ALP_incHN
        Print(Date_Opened, "Date Opened", rid)
        If currentscenario.periodid = 2017181 And (F100_AllAcademies = 17181 Or F100_AllAcademies = 17182 Or F100_AllAcademies = 17183) then
            result = NOR_1617_Scenario_Report_P32b_Total_NOR_ALPincHN
        Else
            Exclude(rid)
        End If

        Return result
    End Function

    <Calculation(Id:="2d7839afe6114a5e86c02c6b6caacdaf")>
    Public Function NOR_P43_PRI As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim NOR_P22 As Decimal = NOR_P22_Total_NOR_PRI_SBS
        Dim F100_AllAcademies As Decimal = [F100_AllAcademies]
        If currentscenario.periodid = 2017181 And (F100_AllAcademies = 17181 Or F100_AllAcademies = 17182 Or F100_AllAcademies = 17183) then
            result = NOR_P22
        Else
            Exclude(rid)
        End If

        Return result
    End Function

    <Calculation(Id:="6a5a8fc8bc504fdb8ad9c89e376cbffb")>
    Public Function NOR_P46_SEC As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim NOR_P25 As Decimal = NOR_P25_Total_NOR_SEC_SBS
        Dim F100_AllAcademies As Decimal = F100_AllAcademies
        If currentscenario.periodid = 2017181 And (F100_AllAcademies = 17181 Or F100_AllAcademies = 17182 Or F100_AllAcademies = 17183) then
            result = NOR_P25
        Else
            Exclude(rid)
        End If

        Return result
    End Function

    <Calculation(Id:="9c91c3eb4d394639bd76eb4c8c2a672c")>
    Public Function NOR_P47_KS3 As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim NOR_P23 As Decimal = NOR_P23_Total_NOR_KS3_SBS
        Dim F100_AllAcademies As Decimal = F100_AllAcademies
        If currentscenario.periodid = 2017181 And (F100_AllAcademies = 17181 Or F100_AllAcademies = 17182 Or F100_AllAcademies = 17183) then
            result = NOR_P23
        Else
            Exclude(rid)
        End If

        Return result
    End Function

    <Calculation(Id:="29f3aa595db648b08849c7aa6b3679ef")>
    Public Function NOR_P48_KS4 As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim NOR_P24 As Decimal = NOR_P24_Total_NOR_KS4_SBS
        Dim F100_AllAcademies As Decimal = F100_AllAcademies
        If currentscenario.periodid = 2017181 And (F100_AllAcademies = 17181 Or F100_AllAcademies = 17182 Or F100_AllAcademies = 17183) then
            result = NOR_P24
        Else
            Exclude(rid)
        End If

        Return result
    End Function

    <Calculation(Id:="02c61627382846f791042fdfeb652257")>
    Public Function NOR_P49_Y1toY4 As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim NOR_P03 As Decimal = NOR_P03_Y1Y4
        Dim F100_AllAcademies As Decimal = F100_AllAcademies
        If currentscenario.periodid = 2017181 And (F100_AllAcademies = 17181 Or F100_AllAcademies = 17182 Or F100_AllAcademies = 17183) then
            result = NOR_P03
        Else
            Exclude(rid)
        End If

        Return result
    End Function

    <Calculation(Id:="36d6dc0a709e4ae78f4a038200b525ba")>
    Public Function NOR_P50_Y5toY6 As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim NOR_P04 As Decimal = NOR_P04_Y5Y6
        Dim F100_AllAcademies As Decimal = F100_AllAcademies
        If currentscenario.periodid = 2017181 And (F100_AllAcademies = 17181 Or F100_AllAcademies = 17182 Or F100_AllAcademies = 17183) then
            result = NOR_P04
        Else
            Exclude(rid)
        End If

        Return result
    End Function
End Class
