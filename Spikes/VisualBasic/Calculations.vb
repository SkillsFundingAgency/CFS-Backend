Imports System

Public Class Calculations
    Inherits BaseCalculation

    Public Property Datasets As Datasets

    <Calculation(Id:="3da67347421d46cfb6fa0538459819f0")>
    <CalculationSpecification(Id:="P004_PriRate", Name:="P004_PriRate")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="bb411f3c107a40a8a71ff57604296e0e", Name:="Basic Entitlement")>
    Public Function P004_PriRate As Decimal
        Dim result As Decimal = 0
        Dim acadfilter As Decimal = F100_AllAcademies
        Dim FundingBasis As String = Datasets.AcademyInformation.FundingBasis
        Dim P004_PriRate_Local As Decimal = LAtoProv(Datasets.APTProformadataset.BasicEntitlementPrimaryAmountPerPupil)
        If FundingBasis = "Place" Then
            exclude(rid)
        Else
            If acadfilter = 17181 Or acadfilter = 1712 Or acadfilter = 17183 Then
                result = P004_PriRate_Local
            Else
                exclude(rid)
            End If
        End If

        Return result
    End Function

    <Calculation(Id:="141cf361fbe942938e7d966263d53b1a")>
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

    <Calculation(Id:="e31d5084077846d58ed2681d0cecd41f")>
    <CalculationSpecification(Id:="P006_NSEN_PriBE", Name:="P006_NSEN_PriBE")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="bb411f3c107a40a8a71ff57604296e0e", Name:="Basic Entitlement")>
    Public Function P006_NSEN_PriBE As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim P005_PriBESubtotal As Decimal = P005_PriBESubtotal
        Dim P006_NSEN_PriBE_Local As Decimal = P005_PriBESubtotal * P006a_NSEN_PriBE_percent
        Dim acadfilter As Decimal = F100_AllAcademies
        Dim FundingBasis As String = Datasets.AcademyInformation.FundingBasis
        If FundingBasis = "Place" Then
            exclude(rid)
        Else
            If acadfilter = 17181 Or acadfilter = 17182 Or acadfilter = 17183 Then
                Result = P006_NSEN_PriBE_Local
            Else
                exclude(rid)
            End If
        End If

        Return result
    End Function

    <Calculation(Id:="dc8dfdd6b305447680630404cb4b494f")>
    <CalculationSpecification(Id:="P006a_NSEN_PriBE_Percent", Name:="P006a_NSEN_PriBE_Percent")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="bb411f3c107a40a8a71ff57604296e0e", Name:="Basic Entitlement")>
    Public Function P006a_NSEN_PriBE_Percent As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="294d81ab8a5840f4841de8cbcd398ef2")>
    <CalculationSpecification(Id:="P007_InYearPriBE_Subtotal", Name:="P007_InYearPriBE_Subtotal")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="bb411f3c107a40a8a71ff57604296e0e", Name:="Basic Entitlement")>
    Public Function P007_InYearPriBE_Subtotal As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim P25_YearDays_1718 As Decimal = P025_YearDays_1718
        Print(P005_PriBESubtotal, "P005_PriBESubtotal", rid)
        Print(P001_1718DaysOpen, "P001_1718DaysOpen", rid)
        Print(P25_YearDays_1718, "P25_YearDays_1718", rid)
        Print(F200_SBS_Academies, "F200_SBS_Academies", rid)
        Result =(P005_PriBESubtotal) * P001_1718DaysOpen / P25_YearDays_1718
        Return result
    End Function

    <Calculation(Id:="c2dce700c37b41efbc84eae66c1d0d10")>
    <CalculationSpecification(Id:="P009_KS3Rate", Name:="P009_KS3Rate")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="bb411f3c107a40a8a71ff57604296e0e", Name:="Basic Entitlement")>
    Public Function P009_KS3Rate As Decimal
        Dim result = Decimal.Zero
        Dim acadfilter As Decimal = F100_AllAcademies
        Dim FundingBasis As String = Datasets.AcademyInformation.FundingBasis
        Dim P009_KS3Rate_Local As Decimal = LAtoProv(Datasets.APTProformadataset.BasicEntitlementKS3AmountPerPupil)
        If FundingBasis = "Place" Then
            exclude(rid)
        Else
            If acadfilter = 17181 Or acadfilter = 17182 Or acadfilter = 17183 Then
                result = P009_KS3Rate_Local
            Else
                Exclude(rid)
            End If
        End If

        Return result
    End Function

    <Calculation(Id:="edaf7b6c9fe34dae9b485b37953ab569")>
    <CalculationSpecification(Id:="P010_KS3_BESubtotal", Name:="P010_KS3_BESubtotal")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="bb411f3c107a40a8a71ff57604296e0e", Name:="Basic Entitlement")>
    Public Function P010_KS3_BESubtotal As Decimal
        Dim result = Decimal.Zero
        Dim FundingBasis As String = Datasets.AcademyInformation.FundingBasis
        Dim AcadFilter As Decimal = F200_SBS_Academies
        Dim P008_KS3Pupils As Decimal = NOR_P23_Total_NOR_KS3_SBS
        Dim P010_KS3_BESubtotal_Local As Decimal =(P008_KS3Pupils * P009_KS3Rate)
        Dim P010_KS3_BESubtotal_LocalAPT As Decimal = Datasets.APTNewISBdataset.BasicEntitlementKS3
        Print(F100_AllAcademies, "F100_AllAcademies", rid)
        Print(FundingBasis, "FundingBasis", rid)
        Print(P008_KS3Pupils, "P008_KS3Pupils", rid)
        Print(P009_KS3Rate, "P009_KS3Rate", rid)
        If FundingBasis = "Place" Then
            exclude(rid)
        Else
            If F100_AllAcademies = 17181 Or (F100_AllAcademies = 17182 And FundingBasis = "Estimate") Or (F100_AllAcademies = 17183 And FundingBasis = "Estimate") THEN
                Result = P010_KS3_BESubtotal_Local
            Else
                If(F100_AllAcademies = 17182 And FundingBasis = "Census") Or (F100_AllAcademies = 17183 And FundingBasis = "Census") THEN
                    Print(P010_KS3_BESubtotal_LocalAPT, "P010_KS3_BESubtotal_LocalAPT", rid)
                    Result = P010_KS3_BESubtotal_LocalAPT
                Else
                    exclude(rid)
                End If
            End If
        End If

        Return result
    End Function

    <Calculation(Id:="63fac834245a40e698f14e5499704999")>
    <CalculationSpecification(Id:="P011_NSEN_KS3BE_percent", Name:="P011_NSEN_KS3BE_percent")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="bb411f3c107a40a8a71ff57604296e0e", Name:="Basic Entitlement")>
    Public Function P011_NSEN_KS3BE_percent As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim acadfilter As Decimal = F100_AllAcademies
        Dim FundingBasis As String = Datasets.AcademyInformation.FundingBasis
        Dim P010_KS3_BESubtotal As Decimal = P010_KS3_BESubtotal
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

    <Calculation(Id:="7590504d07fa4d60b7a31bac686db594")>
    <CalculationSpecification(Id:="P011a_NSEN_KS3BE_Percent", Name:="P011a_NSEN_KS3BE_Percent")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="bb411f3c107a40a8a71ff57604296e0e", Name:="Basic Entitlement")>
    Public Function P011a_NSEN_KS3BE_Percent As Decimal
        Dim result = Decimal.Zero
        Dim acadfilter As Decimal = F100_AllAcademies
        Dim FundingBasis As String = Datasets.AcademyInformation.FundingBasis
        Dim NSEN_KS3BE_Percent As Decimal = LAtoProv(Datasets.APTProformadataset.BasicEntitlementKS3NotionalSEN)
        Dim P011a_NSEN_KS3BE_Percent_Local As Decimal = NSEN_KS3BE_Percent * 100
        If FundingBasis = "Place" Then
            exclude(rid)
        Else
            If acadfilter = 17181 Or acadfilter = 17182 Or acadfilter = 17183 Then
                Result = P011a_NSEN_KS3BE_Percent_Local
            Else
                Exclude(rid)
            End If
        End If

        Return result
    End Function

    <Calculation(Id:="45a4c85151d347469e3254c1273910e0")>
    <CalculationSpecification(Id:="P012_InYearKS3_BESubtotal", Name:="P012_InYearKS3_BESubtotal")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="bb411f3c107a40a8a71ff57604296e0e", Name:="Basic Entitlement")>
    Public Function P012_InYearKS3_BESubtotal As Decimal
        Dim result = Decimal.Zero
        Dim acadfilter As Decimal = F100_AllAcademies
        Dim FundingBasis As String = Datasets.AcademyInformation.FundingBasis
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

    <Calculation(Id:="33be9dd24f6d46c98f4152577903f5f6")>
    <CalculationSpecification(Id:="P014_KS4Rate", Name:="P014_KS4Rate")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="bb411f3c107a40a8a71ff57604296e0e", Name:="Basic Entitlement")>
    Public Function P014_KS4Rate As Decimal
        Dim result = Decimal.Zero
        Dim acadfilter As Decimal = F100_AllAcademies
        Dim FundingBasis As String = Datasets.AcademyInformation.FundingBasis
        Dim P014_KS4Rate_Local As Decimal = LAtoProv(Datasets.APTProformadataset.BasicEntitlementKS4AmountPerPupil)
        If FundingBasis = "Place" Then
            exclude(rid)
        Else
            If acadfilter = 17181 Or acadfilter = 17182 Or acadfilter = 17183 Then
                result = P014_KS4Rate_Local
            Else
                Exclude(rid)
            End If
        End If

        Return result
    End Function

    <Calculation(Id:="9993eec9654d4b62ab997df53b483278")>
    <CalculationSpecification(Id:="P015_KS4_BESubtotal", Name:="P015_KS4_BESubtotal")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="bb411f3c107a40a8a71ff57604296e0e", Name:="Basic Entitlement")>
    Public Function P015_KS4_BESubtotal As Decimal
        Dim result = Decimal.Zero
        Dim FundingBasis As String = Datasets.AcademyInformation.FundingBasis
        Dim P013_KS4Pupils As Decimal = NOR_P24_Total_NOR_KS4_SBS
        Dim P015_KS4_BESubtotal_Local As Decimal =(P013_KS4Pupils * P014_KS4Rate)
        Dim P015_KS4_BESubtotal_LocalAPT As Decimal = Datasets.APTNewISBdataset.BasicEntitlementKS4
        Print(F100_AllAcademies, "F100_AllAcademies", rid)
        Print(P013_KS4Pupils, "P013_KS4Pupils", rid)
        Print(P014_KS4Rate, "P014_KS4Rate", rid)
        Print(P015_KS4_BESubtotal_Local, "P015_KS4_BESubtotal_Local", rid)
        If FundingBasis = "Place" Then
            exclude(rid)
        Else
            If F100_AllAcademies = 17181 Or (F100_AllAcademies = 17182 And FundingBasis = "Estimate") Or (F100_AllAcademies = 17183 And FundingBasis = "Estimate") THEN
                Result = P015_KS4_BESubtotal_Local
            Else
                If(F100_AllAcademies = 17182 And FundingBasis = "Census") Or (F100_AllAcademies = 17183 And FundingBasis = "Census") THEN
                    Result = P015_KS4_BESubtotal_LocalAPT
                Else
                    exclude(rid)
                End If
            End If
        End If

        Return result
    End Function

    <Calculation(Id:="40e1a7bdadf44122a03823cd2a8a3a2b")>
    <CalculationSpecification(Id:="P016_NSEN_KS4BE", Name:="P016_NSEN_KS4BE")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="bb411f3c107a40a8a71ff57604296e0e", Name:="Basic Entitlement")>
    Public Function P016_NSEN_KS4BE As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim acadfilter As Decimal = F100_AllAcademies
        Dim FundingBasis As String = Datasets.AcademyInformation.FundingBasis
        Dim P015_KS4_BESubtotal As Decimal = P015_KS4_BESubtotal
        Dim P016_NSEN_KS4BE_Local As Decimal = P015_KS4_BESubtotal * P016a_NSEN_KS4BE_percent
        Print(P015_KS4_BESubtotal, "P015_KS4_BESubtotal", rid)
        Print(P016a_NSEN_KS4BE_percent, "P016a_NSEN_KS4BE_percent", rid)
        If FundingBasis = "Place" Then
            exclude(rid)
        Else
            If acadfilter = 17181 Or acadfilter = 17182 Or acadfilter = 17183 Then
                Result = P016_NSEN_KS4BE_Local / 100
            Else
                Exclude(rid)
            End If
        End If

        Return result
    End Function

    <Calculation(Id:="e7cd8b1704514f2cbbd21c90c4129385")>
    <CalculationSpecification(Id:="P016a_NSEN_KS4BE_Percent", Name:="P016a_NSEN_KS4BE_Percent")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="bb411f3c107a40a8a71ff57604296e0e", Name:="Basic Entitlement")>
    Public Function P016a_NSEN_KS4BE_Percent As Decimal
        Dim result = Decimal.Zero
        Dim NSEN_KS4BE_Percent As Decimal = LAtoProv(Datasets.APTProformadataset.BasicEntitlementKS4NotionalSEN)
        Dim P016a_NSEN_KS4BE_Percent_Local As Decimal = NSEN_KS4BE_Percent * 100
        If(F200_SBS_Academies = 1) Then
            Result = P016a_NSEN_KS4BE_Percent_Local
        Else
            Exclude(rid)
        End if

        Print(F200_SBS_Academies, "F200_SBS_Academies", rid)
        Print(NSEN_KS4BE_Percent, "NSEN_KS4BE_Percent", rid)
        Print(P016a_NSEN_KS4BE_Percent_Local, "P016a_NSEN_KS4BE_Percent_Local", rid)
        Return result
    End Function

    <Calculation(Id:="19cb38d1e93843678a0123ae79daaead")>
    <CalculationSpecification(Id:="P018_InYearKS4_BESubtotal", Name:="P018_InYearKS4_BESubtotal")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="bb411f3c107a40a8a71ff57604296e0e", Name:="Basic Entitlement")>
    Public Function P018_InYearKS4_BESubtotal As Decimal
        Dim result = Decimal.Zero
        Dim acadfilter As Decimal = F100_AllAcademies
        Dim FundingBasis As String = Datasets.AcademyInformation.FundingBasis
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

    <Calculation(Id:="dd0af51a61f3467ca194245359656960")>
    <CalculationSpecification(Id:="P297_DedelegationRetained", Name:="P297_DedelegationRetained")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="cc9eac91ede6480eb46f748efb3a9d11", Name:="Dedelegation Retained by LA")>
    Public Function P297_DedelegationRetained As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim FundingBasis As String = Datasets.AcademyInformation.FundingBasis
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

    <Calculation(Id:="84630a99352a4cda95daea7b35e8fd31")>
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

    <Calculation(Id:="756fe0d91e454525a6879f31fdbff03b")>
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

    <Calculation(Id:="3a40523062d84ad98287b19211477891")>
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

    <Calculation(Id:="f6a821b15b554fde9864a909db25bf99")>
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

    <Calculation(Id:="306fa1ded2374f0c8ca3205024411536")>
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

    <Calculation(Id:="eb2643c2182b49239403a368694fa141")>
    <CalculationSpecification(Id:="P149_EAL2PriRate", Name:="P149_EAL2PriRate")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="dfd825e4a5784fa188379b080fa0c4ae", Name:="EAL")>
    Public Function P149_EAL2PriRate As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim FundingBasis As Decimal = F900_FundingBasis
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

    <Calculation(Id:="a9db2ddec8a74376a832e08b0ba1413c")>
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

    <Calculation(Id:="3e48982700a942ddba217c743f4f5d99")>
    <CalculationSpecification(Id:="P151_InYearEAL2PriSubtotal", Name:="P151_InYearEAL2PriSubtotal")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="dfd825e4a5784fa188379b080fa0c4ae", Name:="EAL")>
    Public Function P151_InYearEAL2PriSubtotal As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim FundingBasis As Decimal = F900_FundingBasis
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

    <Calculation(Id:="168634538f8e40fdbdd79c956d037bea")>
    <CalculationSpecification(Id:="P152_EAL3PriFactor", Name:="P152_EAL3PriFactor")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="dfd825e4a5784fa188379b080fa0c4ae", Name:="EAL")>
    Public Function P152_EAL3PriFactor As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim FundingBasis As decimal = F900_FundingBasis
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

    <Calculation(Id:="b2b9125d4b3047e49b80082487aa1178")>
    <CalculationSpecification(Id:="P154_EAL3PriRate", Name:="P154_EAL3PriRate")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="dfd825e4a5784fa188379b080fa0c4ae", Name:="EAL")>
    Public Function P154_EAL3PriRate As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim FundingBasis As Decimal = F900_FundingBasis
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

    <Calculation(Id:="93854f294239450f9c81ce6e0dc2e2bf")>
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

    <Calculation(Id:="1c12128f532548ebaf44403bc11d34bd")>
    <CalculationSpecification(Id:="P156_NSENPriEAL", Name:="P156_NSENPriEAL")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="dfd825e4a5784fa188379b080fa0c4ae", Name:="EAL")>
    Public Function P156_NSENPriEAL As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim FundingBasis As Decimal = F900_FundingBasis
        Dim P145_EAL1PriSubtotal As Decimal = P145_EAL1PriSubtotal
        Dim P150_EAL2PriSubtotal As Decimal = P150_EAL2PriSubtotal
        Dim P155_EAL3PriSubtotal As Decimal = P155_EAL3PriSubtotal
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

    <Calculation(Id:="6eba0adf99f045f7a518d44219fdaf3c")>
    <CalculationSpecification(Id:="P156a_NSENPriEAL_Percent", Name:="P156a_NSENPriEAL_Percent")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="dfd825e4a5784fa188379b080fa0c4ae", Name:="EAL")>
    Public Function P156a_NSENPriEAL_Percent As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim FundingBasis As Decimal = F900_FundingBasis
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

    <Calculation(Id:="7708986f8c65463e8083b94a07cea6be")>
    <CalculationSpecification(Id:="P157_InYearEAL3PriSubtotal", Name:="P157_InYearEAL3PriSubtotal")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="dfd825e4a5784fa188379b080fa0c4ae", Name:="EAL")>
    Public Function P157_InYearEAL3PriSubtotal As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim FundingBasis As Decimal = F900_FundingBasis
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

    <Calculation(Id:="9748c63a7cf34efe9074b778298c14c1")>
    <CalculationSpecification(Id:="P158_EAL1SecFactor", Name:="P158_EAL1SecFactor")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="dfd825e4a5784fa188379b080fa0c4ae", Name:="EAL")>
    Public Function P158_EAL1SecFactor As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim FundingBasis As Decimal = F900_FundingBasis
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

    <Calculation(Id:="65681f90d1e54c7d9010a750417d6f19")>
    <CalculationSpecification(Id:="P160_EAL1SecRate", Name:="P160_EAL1SecRate")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="dfd825e4a5784fa188379b080fa0c4ae", Name:="EAL")>
    Public Function P160_EAL1SecRate As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim FundingBasis As Decimal = F900_FundingBasis
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

    <Calculation(Id:="3026e7dece674f7a9a76a3b56037b806")>
    <CalculationSpecification(Id:="P161_EAL1SecSubtotal", Name:="P161_EAL1SecSubtotal")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="dfd825e4a5784fa188379b080fa0c4ae", Name:="EAL")>
    Public Function P161_EAL1SecSubtotal As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim FundingBasis As Decimal = F900_FundingBasis
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

    <Calculation(Id:="8c24963cebd44f349c466e71150eeb2e")>
    <CalculationSpecification(Id:="P162_InYearEAL1SecSubtotal", Name:="P162_InYearEAL1SecSubtotal")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="dfd825e4a5784fa188379b080fa0c4ae", Name:="EAL")>
    Public Function P162_InYearEAL1SecSubtotal As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim FundingBasis As Decimal = F900_FundingBasis
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

    <Calculation(Id:="01da8581dda7425bbca22c12bc241dd1")>
    <CalculationSpecification(Id:="P163_EAL2SecFactor", Name:="P163_EAL2SecFactor")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="dfd825e4a5784fa188379b080fa0c4ae", Name:="EAL")>
    Public Function P163_EAL2SecFactor As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim FundingBasis As Decimal = F900_FundingBasis
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

    <Calculation(Id:="2acfaaf855fd4de7af94850bfa37eeaf")>
    <CalculationSpecification(Id:="P165_EAL2SecRate", Name:="P165_EAL2SecRate")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="dfd825e4a5784fa188379b080fa0c4ae", Name:="EAL")>
    Public Function P165_EAL2SecRate As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim FundingBasis As Decimal = F900_FundingBasis
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

    <Calculation(Id:="7e84d94834194e57ae04915cc432b665")>
    <CalculationSpecification(Id:="P166_EAL2SecSubtotal", Name:="P166_EAL2SecSubtotal")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="dfd825e4a5784fa188379b080fa0c4ae", Name:="EAL")>
    Public Function P166_EAL2SecSubtotal As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim FundingBasis As Decimal = F900_FundingBasis
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

    <Calculation(Id:="8d7407e2f00d4dc2b8281e81d8e61de5")>
    <CalculationSpecification(Id:="P167_InYearEAL2SecSubtotal", Name:="P167_InYearEAL2SecSubtotal")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="dfd825e4a5784fa188379b080fa0c4ae", Name:="EAL")>
    Public Function P167_InYearEAL2SecSubtotal As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim FundingBasis As Decimal = F900_FundingBasis
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

    <Calculation(Id:="2edc02fd39b44e919ce2100c8dd093a6")>
    <CalculationSpecification(Id:="P168_EAL3SecFactor", Name:="P168_EAL3SecFactor")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="dfd825e4a5784fa188379b080fa0c4ae", Name:="EAL")>
    Public Function P168_EAL3SecFactor As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim FundingBasis As Decimal = F900_FundingBasis
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

    <Calculation(Id:="b18dc06ea85241f3828e5ffd2d4e9d49")>
    <CalculationSpecification(Id:="P170_EAL3SecRate", Name:="P170_EAL3SecRate")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="dfd825e4a5784fa188379b080fa0c4ae", Name:="EAL")>
    Public Function P170_EAL3SecRate As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim FundingBasis As Decimal = F900_FundingBasis
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

    <Calculation(Id:="14214059ed3b424b98b06776a9b84ebe")>
    <CalculationSpecification(Id:="P171_EAL3SecSubtotal", Name:="P171_EAL3SecSubtotal")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="dfd825e4a5784fa188379b080fa0c4ae", Name:="EAL")>
    Public Function P171_EAL3SecSubtotal As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim FundingBasis As Decimal = F900_FundingBasis
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

    <Calculation(Id:="6b4f48f2ac7e4a689917ed454b75c433")>
    <CalculationSpecification(Id:="P172_NSENSecEAL", Name:="P172_NSENSecEAL")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="dfd825e4a5784fa188379b080fa0c4ae", Name:="EAL")>
    Public Function P172_NSENSecEAL As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim FundingBasis As Decimal = F900_FundingBasis
        Dim P161_EAL1SecSubtotal As Decimal = P161_EAL1SecSubtotal
        Dim P166_EAL2SecSubtotal As Decimal = P166_EAL2SecSubtotal
        Dim P171_EAL3SecSubtotal As Decimal = P171_EAL3SecSubtotal
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

    <Calculation(Id:="672229baba3244a7baea308b61c1ebc6")>
    <CalculationSpecification(Id:="P172a_NSENSecEAL_Percent", Name:="P172a_NSENSecEAL_Percent")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="dfd825e4a5784fa188379b080fa0c4ae", Name:="EAL")>
    Public Function P172a_NSENSecEAL_Percent As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim FundingBasis As String = Datasets.AcademyInformation.FundingBasis
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

    <Calculation(Id:="3b60045024a94eadbda78bef02528a17")>
    <CalculationSpecification(Id:="P173_InYearEAL3SecSubtotal", Name:="P173_InYearEAL3SecSubtotal")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="dfd825e4a5784fa188379b080fa0c4ae", Name:="EAL")>
    Public Function P173_InYearEAL3SecSubtotal As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim FundingBasis As Decimal = F900_FundingBasis
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

    <Calculation(Id:="f9cf61877d604e1e96d361cddae9ac7c")>
    <CalculationSpecification(Id:="P019_PriFSMFactor", Name:="P019_PriFSMFactor")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="524c10bb461b46f3b351b86403168379", Name:="FSM")>
    Public Function P019_PriFSMFactor As Decimal
        Dim result = Decimal.Zero
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

    <Calculation(Id:="18ac1784c82841cfb45aa21f2dfb45f3")>
    <CalculationSpecification(Id:="P021_PriFSMRate", Name:="P021_PriFSMRate")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="524c10bb461b46f3b351b86403168379", Name:="FSM")>
    Public Function P021_PriFSMRate As Decimal
        Dim result = Decimal.Zero
        If(F200_SBS_Academies = 1) Then
            Dim P021_PriFSMRate_Local As Decimal = LAtoProv(i.)
            Dim LAMethod as string = LAtoProv(datasets.Academy_Allocations.AY_2017 / 18.APT.APT_Proforma_dataset.APT_Proforma_dataset.FSM_Primary_FSM / FSM6)
            Print(P021_PriFSMRate_Local, "Pri FSM Per Pupil", rid)
            Print(LAMethod, "FSM/FSM6?", rid)
            If LAMethod = "FSM % Primary" then
                Result = P021_PriFSMRate_Local
            Else
                Result = 0
            end if
        Else
            exclude(rid)
        End if

        Return result
    End Function

    <Calculation(Id:="b679bd10fd674113becfae5e588798e7")>
    <CalculationSpecification(Id:="P022_PriFSMSubtotal", Name:="P022_PriFSMSubtotal")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="524c10bb461b46f3b351b86403168379", Name:="FSM")>
    Public Function P022_PriFSMSubtotal As Decimal
        Dim result = Decimal.Zero
        Dim P22_Total_NOR_Pri_SBS As Decimal = NOR_P22_Total_NOR_PRI_SBS
        Dim P021_PriFSMRate As Decimal = P021_PriFSMRate
        Dim P022_PriFSMSubtotal_Local As Decimal = P22_Total_NOR_Pri_SBS * P021_PriFSMRate * P019_PriFSMFactor
        Dim FSMSelectedbyLA As String = LaToProv(Datasets.APTProformadataset.FSMPrimaryFSMFSM6)
        Dim P022_PriFSMSubtotal_LocalAPT As Decimal = Datasets.APTNewISBdataset.FreeSchoolMealsPrimary
        Print(F100_AllAcademies, "F100_AllAcademies", rid)
        Print(F900_FundingBasis, "F900_FundingBasis", rid)
        Print(P22_Total_NOR_Pri_SBS, "P22_Total_NOR_Pri_SBS", rid)
        Print(P021_PriFSMRate, "P021_PriFSMRate", rid)
        Print(P019_PriFSMFactor, "P019_PriFSMFactor", rid)
        If F900_FundingBasis = 3 Then
            exclude(rid)
        Else
            If(F100_AllAcademies = 17181) Or (F100_AllAcademies = 17182 And F900_FundingBasis = 2) Or (F100_AllAcademies = 17183 And F900_FundingBasis = 2) Then
                Result = P022_PriFSMSubtotal_Local
            Else
                If(F100_AllAcademies = 17182 And F900_FundingBasis = 1) Or (F100_AllAcademies = 17183 And F900_FundingBasis = 1) Then
                    If FSMSelectedbyLA = "FSM % Primary" Then
                        Result = P022_PriFSMSubtotal_LocalAPT
                    End If
                Else
                End If

                Exclude(rid)
            End If
        End If

        Return result
    End Function

    <Calculation(Id:="487dab7855284a4ebfdc8f1426562290")>
    <CalculationSpecification(Id:="P023_InYearPriFSMSubtotal", Name:="P023_InYearPriFSMSubtotal")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="524c10bb461b46f3b351b86403168379", Name:="FSM")>
    Public Function P023_InYearPriFSMSubtotal As Decimal
        Dim result = Decimal.Zero
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

    <Calculation(Id:="789a0c4f37fc4454a1612ae69fe2eb23")>
    <CalculationSpecification(Id:="P024_PriFSM6Factor", Name:="P024_PriFSM6Factor")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="524c10bb461b46f3b351b86403168379", Name:="FSM")>
    Public Function P024_PriFSM6Factor As Decimal
        Dim result = Decimal.Zero
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

    <Calculation(Id:="4fe68dab86104dd5afd6ac8c2ddfc4fe")>
    <CalculationSpecification(Id:="P026_PriFSM6Rate", Name:="P026_PriFSM6Rate")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="524c10bb461b46f3b351b86403168379", Name:="FSM")>
    Public Function P026_PriFSM6Rate As Decimal
        Dim result = Decimal.Zero
        Dim P026_PriFSM6Rate_Local As Decimal = LAtoProv(Datasets.APTProformadataset.FSMPrimaryAmountPerPupil)
        Dim LAMethod as string = LAtoProv(Datasets.APTProformadataset.FSMPrimaryFSMFSM6)
        Print(F200_SBS_Academies, "F200_SBS_Academies", rid)
        Print(F900_FundingBasis, "F900_FundingBasis", rid)
        Print(P026_PriFSM6Rate_Local, "Pri FSM6 Per Pupil", rid)
        Print(LAMethod, "FSM/FSM6?", rid)
        If F900_FundingBasis = 3 Then
            exclude(rid)
        Else
            If F200_SBS_Academies = 1 Then
                If LAMethod = "FSM6 % Primary" then
                    Result = P026_PriFSM6Rate_Local
                Else
                    Result = 0
                End if
            Else
                exclude(rid)
            End if
        End If

        Return result
    End Function

    <Calculation(Id:="a658b64f9b8e4399b51b172203dcd488")>
    <CalculationSpecification(Id:="P027_PriFSM6Subtotal", Name:="P027_PriFSM6Subtotal")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="524c10bb461b46f3b351b86403168379", Name:="FSM")>
    Public Function P027_PriFSM6Subtotal As Decimal
        Dim result = Decimal.Zero
        Dim P22_Total_NOR_Pri_SBS As Decimal = NOR_P22_Total_NOR_PRI_SBS
        Dim P026_PriFSM6Rate As Decimal = P026_PriFSM6Rate
        Dim P027_PriFSM6Subtotal_Local As Decimal = P22_Total_NOR_Pri_SBS * P026_PriFSM6Rate * P024_PriFSM6Factor
        Dim FSM6SelectedbyLA As String = LaToProv(Datasets.APTProformadataset.FSMPrimaryFSMFSM6)
        Dim P027_PriFSM6Subtotal_LocalAPT As Decimal = Datasets.APTNewISBdataset.FreeSchoolMealsPrimary
        Print(F100_AllAcademies, "F100_AllAcademies", rid)
        Print(F900_FundingBasis, "F900_FundingBasis", rid)
        Print(P22_Total_NOR_Pri_SBS, "P22_Total_NOR_Pri_SBS", rid)
        Print(P026_PriFSM6Rate, "P026_PriFSM6Rate", rid)
        Print(P024_PriFSM6Factor, "P024_PriFSM6Factor", rid)
        If F900_FundingBasis = 3 Then
            exclude(rid)
        Else
            If(F100_AllAcademies = 17181) Or (F100_AllAcademies = 17182 And F900_FundingBasis = 2) Or (F100_AllAcademies = 17183 And F900_FundingBasis = 2) Then
                Result = P027_PriFSM6Subtotal_Local
            Else
                If(F100_AllAcademies = 17182 And F900_FundingBasis = 1) Or (F100_AllAcademies = 17183 And F900_FundingBasis = 2) Then
                    If FSM6SelectedbyLA = "FSM6 % Primary" Then
                        Result = P027_PriFSM6Subtotal_LocalAPT
                    End If
                Else
                End If

                Exclude(rid)
            End If
        End If

        Return result
    End Function

    <Calculation(Id:="67b4ac18106545a7a87283b6d632e6e5")>
    <CalculationSpecification(Id:="P028_NSENFSMPri", Name:="P028_NSENFSMPri")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="524c10bb461b46f3b351b86403168379", Name:="FSM")>
    Public Function P028_NSENFSMPri As Decimal
        Dim result = Decimal.Zero
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

    <Calculation(Id:="ae4898ce222a4588bb6d513d6117566d")>
    <CalculationSpecification(Id:="P028a_NSENFSMPri_Percent", Name:="P028a_NSENFSMPri_Percent")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="524c10bb461b46f3b351b86403168379", Name:="FSM")>
    Public Function P028a_NSENFSMPri_Percent As Decimal
        Dim result = Decimal.Zero
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

    <Calculation(Id:="d07a9713c3df46618275dc8865a42c84")>
    <CalculationSpecification(Id:="P029_InYearPriFSM6Subtotal", Name:="P029_InYearPriFSM6Subtotal")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="524c10bb461b46f3b351b86403168379", Name:="FSM")>
    Public Function P029_InYearPriFSM6Subtotal As Decimal
        Dim result = Decimal.Zero
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

    <Calculation(Id:="0dffbb472c194fb7be923edea1c5fd5c")>
    <CalculationSpecification(Id:="P030_SecFSMFactor", Name:="P030_SecFSMFactor")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="524c10bb461b46f3b351b86403168379", Name:="FSM")>
    Public Function P030_SecFSMFactor As Decimal
        Dim result = Decimal.Zero
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

    <Calculation(Id:="676302c2b9c1493d9f343707d9fb98de")>
    <CalculationSpecification(Id:="P032_SecFSMRate", Name:="P032_SecFSMRate")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="524c10bb461b46f3b351b86403168379", Name:="FSM")>
    Public Function P032_SecFSMRate As Decimal
        Dim result = Decimal.Zero
        Dim P032_SecFSMRate_Local As Decimal = LAtoProv(Datasets.APTProformadataset.FSMSecondaryAmountPerPupil)
        Dim LAMethod as string = LAtoProv(Datasets.APTProformadataset.FSMSecondaryFSMFSM6)
        Print(F200_SBS_Academies, "F200_SBS_Academies", rid)
        Print(F900_FundingBasis, "F900_FundingBasis", rid)
        Print(P032_SecFSMRate_Local, "Sec FSM Per Pupil", rid)
        Print(LAMethod, "FSM/FSM6?", rid)
        If F900_FundingBasis = 3 Then
            exclude(rid)
        Else
            If F200_SBS_Academies = 1 Then
                If LAMethod = "FSM % Secondary" then
                    Result = P032_SecFSMRate_Local
                Else
                    Result = 0
                End if
            Else
                Exclude(rid)
            End if
        End If

        Return result
    End Function

    <Calculation(Id:="3d82bf1fae93405e910462305a6023a9")>
    <CalculationSpecification(Id:="P033_SecFSMSubtotal", Name:="P033_SecFSMSubtotal")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="524c10bb461b46f3b351b86403168379", Name:="FSM")>
    Public Function P033_SecFSMSubtotal As Decimal
        Dim result = Decimal.Zero
        Dim P25_TotalNORSecSBS As Decimal = NOR_P25_Total_NOR_SEC_SBS
        Dim P032_SecFSMRate As Decimal = P032_SecFSMRate
        Dim P033_SecFSMSubtotal_Local As Decimal = P25_TotalNORSecSBS * P032_SecFSMRate * P030_SecFSMFactor
        Dim FSMSelectedbyLA As String = LaToProv(Datasets.APTProformadataset.FSMSecondaryFSMFSM6)
        Dim P033_SecFSMSubtotal_LocalAPT As Decimal = Datasets.APTNewISBdataset.FreeSchoolMealsSecondary
        Print(F100_AllAcademies, "F100_AllAcademies", rid)
        Print(F900_FundingBasis, "F900_FundingBasis", rid)
        Print(P25_TotalNORSecSBS, "P25_TotalNORSecSBS", rid)
        Print(P032_SecFSMRate, "P032_SecFSMRate", rid)
        Print(P030_SecFSMFactor, "P030_SecFSMFactor", rid)
        Print(FSMSelectedbyLA, "FSMSelectedbyLA", rid)
        Print(P033_SecFSMSubtotal_LocalAPT, "P033_SecFSMSubtotal_LocalAPT", rid)
        If F900_FundingBasis = 3 Then
            Exclude(rid)
        Else
        End if

        If F100_AllAcademies = 17181 Or (F100_AllAcademies = 17182 And F900_FundingBasis = 2) Or (F100_AllAcademies = 17183 And F900_FundingBasis = 2) Then
            Result = P033_SecFSMSubtotal_Local
        Else
            If(F100_AllAcademies = 17182 And F900_FundingBasis = 1) Or (F100_AllAcademies = 17183 And F900_FundingBasis = 1) Then
                If FSMSelectedbyLA = "FSM % Secondary" Then
                    Result = P033_SecFSMSubtotal_LocalAPT
                Else
                    Result = 0
                End If
            Else
                Exclude(rid)
            End If
        End if

        Return result
    End Function

    <Calculation(Id:="837fd74f01b64645b1589eef74f9e980")>
    <CalculationSpecification(Id:="P034_InYearSecFSMSubtotal", Name:="P034_InYearSecFSMSubtotal")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="524c10bb461b46f3b351b86403168379", Name:="FSM")>
    Public Function P034_InYearSecFSMSubtotal As Decimal
        Dim result = Decimal.Zero
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

    <Calculation(Id:="c98e058478da453b86bc436e20d50d0d")>
    <CalculationSpecification(Id:="P035_SecFSM6Factor", Name:="P035_SecFSM6Factor")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="524c10bb461b46f3b351b86403168379", Name:="FSM")>
    Public Function P035_SecFSM6Factor As Decimal
        Dim result = Decimal.Zero
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

    <Calculation(Id:="8384dc1db45e43209d5dba6d2dcec88d")>
    <CalculationSpecification(Id:="P037_SecFSM6Rate", Name:="P037_SecFSM6Rate")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="524c10bb461b46f3b351b86403168379", Name:="FSM")>
    Public Function P037_SecFSM6Rate As Decimal
        Dim result = Decimal.Zero
        Dim P037_SecFSM6Rate_Local As Decimal = LAtoProv(Datasets.APTProformadataset.FSMSecondaryAmountPerPupil)
        Dim LAMethod as string = LAtoProv(Datasets.APTProformadataset.FSMSecondaryFSMFSM6)
        Print(F200_SBS_Academies, "F200_SBS_Academies", rid)
        Print(F900_FundingBasis, "F900_FundingBasis", rid)
        Print(P037_SecFSM6Rate_Local, "Sec FSM6 Per Pupil", rid)
        Print(LAMethod, "FSM/FSM6?", rid)
        If F900_FundingBasis = 3 Then
            exclude(rid)
        Else
            If F200_SBS_Academies = 1 Then
                If LAMethod = "FSM6 % Secondary" Then
                    Result = P037_SecFSM6Rate_Local
                Else
                    Result = 0
                End if
            Else
                Exclude(rid)
            End if
        End If

        Return result
    End Function

    <Calculation(Id:="143aa25bb5c04ecbb1dc52b2b987df59")>
    <CalculationSpecification(Id:="P038_SecFSM6Subtotal", Name:="P038_SecFSM6Subtotal")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="524c10bb461b46f3b351b86403168379", Name:="FSM")>
    Public Function P038_SecFSM6Subtotal As Decimal
        Dim result = Decimal.Zero
        Dim P25_Total_NOR_Sec_SBS As Decimal = NOR_P25_Total_NOR_SEC_SBS
        Dim P037_SecFSM6Rate As Decimal = P037_SecFSM6Rate
        Dim P038_SecFSM6Subtotal_Local As Decimal = P25_Total_NOR_Sec_SBS * P037_SecFSM6Rate * P035_SecFSM6Factor
        Dim FSMSelectedbyLA As String = LaToProv(Datasets.APTProformadataset.FSMSecondaryFSMFSM6)
        Dim P038_SecFSM6Subtotal_LocalAPT As Decimal = Datasets.APTNewISBdataset.FreeSchoolMealsSecondary
        Print(P038_SecFSM6Subtotal_LocalAPT, "P038_SecFSM6Subtotal_LocalAPT", rid)
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
            Result = P038_SecFSM6Subtotal_Local
        Else
            If(F100_AllAcademies = 17182 And F900_FundingBasis = 1) Or (F100_AllAcademies = 17183 And F900_FundingBasis = 1) Then
                If FSMSelectedbyLA = "FSM6 % Secondary" Then
                    Result = P038_SecFSM6Subtotal_LocalAPT
                Else
                    Result = 0
                End If
            Else
                exclude(rid)
            End If
        End if

        Return result
    End Function

    <Calculation(Id:="9f6011998fd641f688eccd9878c61326")>
    <CalculationSpecification(Id:="P039_NSENFSMSec", Name:="P039_NSENFSMSec")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="524c10bb461b46f3b351b86403168379", Name:="FSM")>
    Public Function P039_NSENFSMSec As Decimal
        Dim result = Decimal.Zero
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

    <Calculation(Id:="d4415fd3ca3148f0afb914a364654849")>
    <CalculationSpecification(Id:="P039a_NSENFSMSec_Percent", Name:="P039a_NSENFSMSec_Percent")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="524c10bb461b46f3b351b86403168379", Name:="FSM")>
    Public Function P039a_NSENFSMSec_Percent As Decimal
        Dim result = Decimal.Zero
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

    <Calculation(Id:="4d5654e1dfd94e4e904d617639b0ff63")>
    <CalculationSpecification(Id:="P040_InYearSecFSM6Subtotal", Name:="P040_InYearSecFSM6Subtotal")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="524c10bb461b46f3b351b86403168379", Name:="FSM")>
    Public Function P040_InYearSecFSM6Subtotal As Decimal
        Dim result = Decimal.Zero
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

    <Calculation(Id:="d263f6f44eb0449bba1520e2f479efd1")>
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

    <Calculation(Id:="2237e9a9ea2343f790f04f711bdbd8ea")>
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

    <Calculation(Id:="ce3f19a096f24ae8a809308300d2adc0")>
    <CalculationSpecification(Id:="P044_IDACIFPriSubtotal", Name:="P044_IDACIFPriSubtotal")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="86e2181721da4a669a09cccbafc502f1", Name:="IDACI")>
    Public Function P044_IDACIFPriSubtotal As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim SBS_P22 As Decimal = NOR_P22_Total_NOR_PRI_SBS
        Dim APT_ISB_IDACIF_Primary As Decimal = Datasets.APTNewISBdataset.IDACI(PF)
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

    <Calculation(Id:="ff486d8ef9d5408e8ee8f223866f7c0f")>
    <CalculationSpecification(Id:="P045_NSENIDACIFPri", Name:="P045_NSENIDACIFPri")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="86e2181721da4a669a09cccbafc502f1", Name:="IDACI")>
    Public Function P045_NSENIDACIFPri As Decimal
        Dim result As Decimal = 0 'change to As String if text product
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

    <Calculation(Id:="3671a2dc24424da3b6dc23319079a46a")>
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

    <Calculation(Id:="f2ae61bf0a954e3180126b6931093bdc")>
    <CalculationSpecification(Id:="P046_InYearIDACIFPriSubtotal", Name:="P046_InYearIDACIFPriSubtotal")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="86e2181721da4a669a09cccbafc502f1", Name:="IDACI")>
    Public Function P046_InYearIDACIFPriSubtotal As Decimal
        Dim result As Decimal = 0 'change to As String if text product
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

    <Calculation(Id:="1edbaf7f1a664feb9d828c319f2cc106")>
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

    <Calculation(Id:="be4985f86d17440f834eec523d629baf")>
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

    <Calculation(Id:="41da0b6d772f44f6afde336cc2595929")>
    <CalculationSpecification(Id:="P050_IDACIEPriSubtotal", Name:="P050_IDACIEPriSubtotal")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="86e2181721da4a669a09cccbafc502f1", Name:="IDACI")>
    Public Function P050_IDACIEPriSubtotal As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim SBS_P22 As Decimal = NOR_P22_Total_NOR_PRI_SBS
        Dim APT_ISB_IDACIE_Primary As Decimal = Datasets.APTNewISBdataset.IDACI(PE)
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

    <Calculation(Id:="c9584d9b2ebe4443aaa9d33360484297")>
    <CalculationSpecification(Id:="P051_NSENIDACIEPri", Name:="P051_NSENIDACIEPri")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="86e2181721da4a669a09cccbafc502f1", Name:="IDACI")>
    Public Function P051_NSENIDACIEPri As Decimal
        Dim result As Decimal = 0 'change to As String if text product
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

    <Calculation(Id:="16d43c13aab641b1b8d40852da824aff")>
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

    <Calculation(Id:="170a54a688c14c72bb68771aa09e43b6")>
    <CalculationSpecification(Id:="P052_InYearIDACIEPriSubtotal", Name:="P052_InYearIDACIEPriSubtotal")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="86e2181721da4a669a09cccbafc502f1", Name:="IDACI")>
    Public Function P052_InYearIDACIEPriSubtotal As Decimal
        Dim result As Decimal = 0 'change to As String if text product
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

    <Calculation(Id:="79d8be41e1b24164b6b88ff9b93ff576")>
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

    <Calculation(Id:="7a9c0e3c89cf419483353040b7edb5df")>
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

    <Calculation(Id:="6d631f5240624c88ab62ee4473dc9fb5")>
    <CalculationSpecification(Id:="P056_IDACIDPriSubtotal", Name:="P056_IDACIDPriSubtotal")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="86e2181721da4a669a09cccbafc502f1", Name:="IDACI")>
    Public Function P056_IDACIDPriSubtotal As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim SBS_P22 As Decimal = NOR_P22_Total_NOR_PRI_SBS
        Dim APT_ISB_IDACID_Primary As Decimal = Datasets.APTNewISBdataset.IDACI(PD)
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

    <Calculation(Id:="0381c020fe4f4594a101ca57e8a32b67")>
    <CalculationSpecification(Id:="P057_NSENIDACIDPri", Name:="P057_NSENIDACIDPri")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="86e2181721da4a669a09cccbafc502f1", Name:="IDACI")>
    Public Function P057_NSENIDACIDPri As Decimal
        Dim result As Decimal = 0 'change to As String if text product
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

    <Calculation(Id:="2356931b3e6149acb474b07039951ece")>
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

    <Calculation(Id:="40659e10b3f04aa5b3800259ed253f83")>
    <CalculationSpecification(Id:="P058_InYearIDACIDPriSubtotal", Name:="P058_InYearIDACIDPriSubtotal")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="86e2181721da4a669a09cccbafc502f1", Name:="IDACI")>
    Public Function P058_InYearIDACIDPriSubtotal As Decimal
        Dim result As Decimal = 0 'change to As String if text product
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

    <Calculation(Id:="80feefc52c254a098d16801772949eb9")>
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

    <Calculation(Id:="7ba462b833ff4fa888de89f665094496")>
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

    <Calculation(Id:="5b23a7812fc54395a7d17ae0ffde5255")>
    <CalculationSpecification(Id:="P062_IDACICPriSubtotal", Name:="P062_IDACICPriSubtotal")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="86e2181721da4a669a09cccbafc502f1", Name:="IDACI")>
    Public Function P062_IDACICPriSubtotal As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim SBS_P22 As Decimal = NOR_P22_Total_NOR_PRI_SBS
        Dim APT_ISB_IDACIC_Primary As Decimal = Datasets.APTNewISBdataset.IDACI(PC)
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

    <Calculation(Id:="1d53d1a5b33b457c8e1caf522dbedc0b")>
    <CalculationSpecification(Id:="P063_NSENIDACICPri", Name:="P063_NSENIDACICPri")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="86e2181721da4a669a09cccbafc502f1", Name:="IDACI")>
    Public Function P063_NSENIDACICPri As Decimal
        Dim result As Decimal = 0 'change to As String if text product
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

    <Calculation(Id:="f5f5c7fbde2b4f8abece27c6b2ca6ffc")>
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

    <Calculation(Id:="dfdfe133049541ec9404867dd5e7baa0")>
    <CalculationSpecification(Id:="P064_InYearIDACICPriSubtotal", Name:="P064_InYearIDACICPriSubtotal")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="86e2181721da4a669a09cccbafc502f1", Name:="IDACI")>
    Public Function P064_InYearIDACICPriSubtotal As Decimal
        Dim result As Decimal = 0 'change to As String if text product
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

    <Calculation(Id:="bceae22ac43544148e71a2261d90febf")>
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

    <Calculation(Id:="6926b471bb594f2aa31a8539123e1b2d")>
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

    <Calculation(Id:="13c9c405e65c48b1a664d28056ca48cc")>
    <CalculationSpecification(Id:="P068_IDACIBPriSubtotal", Name:="P068_IDACIBPriSubtotal")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="86e2181721da4a669a09cccbafc502f1", Name:="IDACI")>
    Public Function P068_IDACIBPriSubtotal As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim SBS_P22 As Decimal = NOR_P22_Total_NOR_PRI_SBS
        Dim APT_ISB_IDACIB_Primary As Decimal = Datasets.APTNewISBdataset.IDACI(PB)
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

    <Calculation(Id:="568507a8be8e4d00bb57ec77189ffe92")>
    <CalculationSpecification(Id:="P069_NSENIDACIBPri", Name:="P069_NSENIDACIBPri")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="86e2181721da4a669a09cccbafc502f1", Name:="IDACI")>
    Public Function P069_NSENIDACIBPri As Decimal
        Dim result As Decimal = 0 'change to As String if text product
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

    <Calculation(Id:="792cdbad9a7944d99e8a392d5653bb95")>
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

    <Calculation(Id:="1c5bb7611c304c72a0892148898eba99")>
    <CalculationSpecification(Id:="P070_InYearIDACIBPriSubtotal", Name:="P070_InYearIDACIBPriSubtotal")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="86e2181721da4a669a09cccbafc502f1", Name:="IDACI")>
    Public Function P070_InYearIDACIBPriSubtotal As Decimal
        Dim result As Decimal = 0 'change to As String if text product
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

    <Calculation(Id:="2128ef4526bb42d5a4a96b4b8ee4a913")>
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

    <Calculation(Id:="a78355351398430785559135a4572a10")>
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

    <Calculation(Id:="ccc1a410988a4656b41973693d4bc345")>
    <CalculationSpecification(Id:="P074_IDACIAPriSubtotal", Name:="P074_IDACIAPriSubtotal")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="86e2181721da4a669a09cccbafc502f1", Name:="IDACI")>
    Public Function P074_IDACIAPriSubtotal As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim SBS_P22 As Decimal = NOR_P22_Total_NOR_PRI_SBS
        Dim APT_ISB_IDACIA_Primary As Decimal = Datasets.APTNewISBdataset.IDACI(PA)
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

    <Calculation(Id:="71bdcecced584e6f9697dcfcf2342600")>
    <CalculationSpecification(Id:="P075_NSENIDACIAPri", Name:="P075_NSENIDACIAPri")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="86e2181721da4a669a09cccbafc502f1", Name:="IDACI")>
    Public Function P075_NSENIDACIAPri As Decimal
        Dim result As Decimal = 0 'change to As String if text product
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

    <Calculation(Id:="412c22ac1ed94d8eaccb83f20ba69b56")>
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

    <Calculation(Id:="67f275fcf8a943ae9b45f206af8a82ab")>
    <CalculationSpecification(Id:="P076_InYearIDACIAPriSubtotal", Name:="P076_InYearIDACIAPriSubtotal")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="86e2181721da4a669a09cccbafc502f1", Name:="IDACI")>
    Public Function P076_InYearIDACIAPriSubtotal As Decimal
        Dim result As Decimal = 0 'change to As String if text product
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

    <Calculation(Id:="af5f2750a3b044288af5a4e6dede06c4")>
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

    <Calculation(Id:="e86abb17a95049d4b8de98806c8467cb")>
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

    <Calculation(Id:="35b0a47b8d8a4b22bfe25d78b279cc21")>
    <CalculationSpecification(Id:="P080_IDACIFSecSubtotal", Name:="P080_IDACIFSecSubtotal")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="86e2181721da4a669a09cccbafc502f1", Name:="IDACI")>
    Public Function P080_IDACIFSecSubtotal As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim SBS_P25 As Decimal = NOR_P25_Total_NOR_SEC_SBS
        Dim APT_ISB_IDACIF_Secondary As Decimal = Datasets.APTNewISBdataset.IDACI(SF)
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

    <Calculation(Id:="565687dfb4684c3b95eac6a443a118cd")>
    <CalculationSpecification(Id:="P081_NSENIDACIFSec", Name:="P081_NSENIDACIFSec")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="86e2181721da4a669a09cccbafc502f1", Name:="IDACI")>
    Public Function P081_NSENIDACIFSec As Decimal
        Dim result As Decimal = 0 'change to As String if text product
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

    <Calculation(Id:="85106b181dc1441dbf8ed52af9ca1235")>
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

    <Calculation(Id:="11fc99ed8906465f8aa4a29a37fc6dbe")>
    <CalculationSpecification(Id:="P082_InYearIDACIFSecSubtotal", Name:="P082_InYearIDACIFSecSubtotal")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="86e2181721da4a669a09cccbafc502f1", Name:="IDACI")>
    Public Function P082_InYearIDACIFSecSubtotal As Decimal
        Dim result As Decimal = 0 'change to As String if text product
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

    <Calculation(Id:="8a90aad5864b4b448f6bce1246b4ed28")>
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

    <Calculation(Id:="8c2b7ccfbfe746a7aa1f3104fe537341")>
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

    <Calculation(Id:="5ed5c79fd9ae40a2879c3721618eea5a")>
    <CalculationSpecification(Id:="P086_IDACIESecSubtotal", Name:="P086_IDACIESecSubtotal")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="86e2181721da4a669a09cccbafc502f1", Name:="IDACI")>
    Public Function P086_IDACIESecSubtotal As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim SBS_P25 As Decimal = NOR_P25_Total_NOR_SEC_SBS
        Dim APT_ISB_IDACIE_Secondary As Decimal = Datasets.APTNewISBdataset.IDACI(SE)
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

    <Calculation(Id:="a027c88e0f594e87b2b56be0e2f85036")>
    <CalculationSpecification(Id:="P087_NSENIDACIESec", Name:="P087_NSENIDACIESec")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="86e2181721da4a669a09cccbafc502f1", Name:="IDACI")>
    Public Function P087_NSENIDACIESec As Decimal
        Dim result As Decimal = 0 'change to As String if text product
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

    <Calculation(Id:="61090a73c3d64a698dfa2ba621e439c5")>
    <CalculationSpecification(Id:="P87a_NSENIDACIESec_Percent", Name:="P87a_NSENIDACIESec_Percent")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="86e2181721da4a669a09cccbafc502f1", Name:="IDACI")>
    Public Function P87a_NSENIDACIESec_Percent As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="af2b3d15c4fc4b08966cb51ba2150ef5")>
    <CalculationSpecification(Id:="P088_InYearIDACIESecSubtotal", Name:="P088_InYearIDACIESecSubtotal")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="86e2181721da4a669a09cccbafc502f1", Name:="IDACI")>
    Public Function P088_InYearIDACIESecSubtotal As Decimal
        Dim result As Decimal = 0 'change to As String if text product
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

    <Calculation(Id:="8df8ea44e25044d7a0fb045f39e5a34c")>
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

    <Calculation(Id:="a5d90c6023a44ad6846ec4159961f853")>
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

    <Calculation(Id:="521ba90295244ed48e6490d6433966f2")>
    <CalculationSpecification(Id:="P092_IDACIDSecSubtotal", Name:="P092_IDACIDSecSubtotal")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="86e2181721da4a669a09cccbafc502f1", Name:="IDACI")>
    Public Function P092_IDACIDSecSubtotal As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim SBS_P25 As Decimal = NOR_P25_Total_NOR_SEC_SBS
        Dim APT_ISB_IDACID_Secondary As Decimal = Datasets.APTNewISBdataset.IDACI(SD)
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

    <Calculation(Id:="2b6a0a0ffa944253a196384628d8285f")>
    <CalculationSpecification(Id:="P093_NSENIDACIDSec", Name:="P093_NSENIDACIDSec")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="86e2181721da4a669a09cccbafc502f1", Name:="IDACI")>
    Public Function P093_NSENIDACIDSec As Decimal
        Dim result As Decimal = 0 'change to As String if text product
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

    <Calculation(Id:="d0727c102a0c499c8bcaceec29951570")>
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

    <Calculation(Id:="d54c8c1368bd459eb09bf88e6db28145")>
    <CalculationSpecification(Id:="P094_InYearIDACIDSecSubtotal", Name:="P094_InYearIDACIDSecSubtotal")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="86e2181721da4a669a09cccbafc502f1", Name:="IDACI")>
    Public Function P094_InYearIDACIDSecSubtotal As Decimal
        Dim result As Decimal = 0 'change to As String if text product
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

    <Calculation(Id:="a73a226bf9494a20a734af7dd787cc78")>
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

    <Calculation(Id:="2bcbb7dd0b44483db70831947b9f9da0")>
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

    <Calculation(Id:="0bc8794cb3c44cb8b9b34a4ff377429d")>
    <CalculationSpecification(Id:="P098_IDACICSecSubtotal", Name:="P098_IDACICSecSubtotal")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="86e2181721da4a669a09cccbafc502f1", Name:="IDACI")>
    Public Function P098_IDACICSecSubtotal As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim SBS_P25 As Decimal = NOR_P25_Total_NOR_SEC_SBS
        Dim APT_ISB_IDACIC_Secondary As Decimal = Datasets.APTNewISBdataset.IDACI(SC)
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

    <Calculation(Id:="0cb4f5d09ae745d39429549c7aff7a57")>
    <CalculationSpecification(Id:="P099_NSENIDACICSec", Name:="P099_NSENIDACICSec")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="86e2181721da4a669a09cccbafc502f1", Name:="IDACI")>
    Public Function P099_NSENIDACICSec As Decimal
        Dim result As Decimal = 0 'change to As String if text product
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

    <Calculation(Id:="a090117aa4bc498c9a42245b528dbe3e")>
    <CalculationSpecification(Id:="P099a_NSENIDACICSec_Percent", Name:="P099a_NSENIDACICSec_Percent")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="86e2181721da4a669a09cccbafc502f1", Name:="IDACI")>
    Public Function P099a_NSENIDACICSec_Percent As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="c252027622f14764ade3088a4a8fab24")>
    <CalculationSpecification(Id:="P100_InYearIDACICSecSubtotal", Name:="P100_InYearIDACICSecSubtotal")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="86e2181721da4a669a09cccbafc502f1", Name:="IDACI")>
    Public Function P100_InYearIDACICSecSubtotal As Decimal
        Dim result As Decimal = 0 'change to As String if text product
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

    <Calculation(Id:="a109a989c97149bc82bb513e5f119042")>
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

    <Calculation(Id:="52614392a0104832a1688203d62aac33")>
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

    <Calculation(Id:="83a194f92d6d48638e6db375b2332630")>
    <CalculationSpecification(Id:="P104_IDACIBSecSubtotal", Name:="P104_IDACIBSecSubtotal")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="86e2181721da4a669a09cccbafc502f1", Name:="IDACI")>
    Public Function P104_IDACIBSecSubtotal As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim SBS_P25 As Decimal = NOR_P25_Total_NOR_SEC_SBS
        Dim APT_ISB_IDACIB_Secondary As Decimal = Datasets.APTNewISBdataset.IDACI(SB)
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

    <Calculation(Id:="3c51c9b844e44064b8fb466485f854eb")>
    <CalculationSpecification(Id:="P105_NSENIDACIBSec", Name:="P105_NSENIDACIBSec")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="86e2181721da4a669a09cccbafc502f1", Name:="IDACI")>
    Public Function P105_NSENIDACIBSec As Decimal
        Dim result As Decimal = 0 'change to As String if text product
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

    <Calculation(Id:="adb280a88566482cba38cc5d581b2824")>
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

    <Calculation(Id:="191b31ad45294ac1a747c502cb3af7db")>
    <CalculationSpecification(Id:="P106_InYearIDACIBSecSubtotal", Name:="P106_InYearIDACIBSecSubtotal")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="86e2181721da4a669a09cccbafc502f1", Name:="IDACI")>
    Public Function P106_InYearIDACIBSecSubtotal As Decimal
        Dim result As Decimal = 0 'change to As String if text product
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

    <Calculation(Id:="7ea83295d266432dbe93c6e6b31af7c2")>
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

    <Calculation(Id:="d2c48c501e6a451d9c84a61035bf8b21")>
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

    <Calculation(Id:="c3a5a64d89c446f38841b44812dfcbb2")>
    <CalculationSpecification(Id:="P110_IDACIASecSubtotal", Name:="P110_IDACIASecSubtotal")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="86e2181721da4a669a09cccbafc502f1", Name:="IDACI")>
    Public Function P110_IDACIASecSubtotal As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="8e9530c9a59142d8bd76730d91a7a547")>
    <CalculationSpecification(Id:="P111_NSENIDACIASec", Name:="P111_NSENIDACIASec")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="86e2181721da4a669a09cccbafc502f1", Name:="IDACI")>
    Public Function P111_NSENIDACIASec As Decimal
        Dim result As Decimal = 0 'change to As String if text product
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

    <Calculation(Id:="93170cc8c1c84bddbea2fd6ea703e32e")>
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

    <Calculation(Id:="1a751b14331e45799267b3857cbf6627")>
    <CalculationSpecification(Id:="P112_InYearIDACIASecSubtotal", Name:="P112_InYearIDACIASecSubtotal")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="86e2181721da4a669a09cccbafc502f1", Name:="IDACI")>
    Public Function P112_InYearIDACIASecSubtotal As Decimal
        Dim result As Decimal = 0 'change to As String if text product
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

    <Calculation(Id:="69c5168b5bdf4ba8b9c6acf873f42b62")>
    <CalculationSpecification(Id:="P114_LACFactor", Name:="P114_LACFactor")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="3e4d786c0a984c6c9eb92a959ef1e5f5", Name:="LAC")>
    Public Function P114_LACFactor As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim FundingBasis As String = F900_FundingBasis
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

    <Calculation(Id:="194d6c6978374d77aabeb1b0cecf6476")>
    <CalculationSpecification(Id:="P116_LACRate", Name:="P116_LACRate")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="3e4d786c0a984c6c9eb92a959ef1e5f5", Name:="LAC")>
    Public Function P116_LACRate As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim FundingBasis As String = F900_FundingBasis
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

    <Calculation(Id:="9634621640ab4f8295e07939db5dca89")>
    <CalculationSpecification(Id:="P117_LACSubtotal", Name:="P117_LACSubtotal")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="3e4d786c0a984c6c9eb92a959ef1e5f5", Name:="LAC")>
    Public Function P117_LACSubtotal As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim FundingBasis As decimal = F900_FundingBasis
        Dim P26_Total_NOR_SBS As Decimal = NOR_P26_Total_NOR_SBS
        Dim P117_LACSubtotal_Local As Decimal = P26_Total_NOR_SBS * P116_LACRate * P114_LACFactor
        Dim LAC As Decimal = Datasets.APTNewISBdataset.IDACI(SA)
        If F200_SBS_Academies <> 1 Then
            exclude(rid)
        End if

        If(F100_AllAcademies = 17181) Or (FundingBasis = 2 And F100_AllAcademies = 17182) Or (FundingBasis = 2 And F100_AllAcademies = 17183) then
            result = P117_LACSubtotal_Local
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

    <Calculation(Id:="bdb886abe8004acb843dbb31ff22ceec")>
    <CalculationSpecification(Id:="P118_NSENLAC", Name:="P118_NSENLAC")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="3e4d786c0a984c6c9eb92a959ef1e5f5", Name:="LAC")>
    Public Function P118_NSENLAC As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim FundingBasis As String = F900_FundingBasis
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

    <Calculation(Id:="029a378f2af14caeb5be59a16c37d773")>
    <CalculationSpecification(Id:="P118a_NSENLAC_Percent", Name:="P118a_NSENLAC_Percent")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="3e4d786c0a984c6c9eb92a959ef1e5f5", Name:="LAC")>
    Public Function P118a_NSENLAC_Percent As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim FundingBasis As String = F900_FundingBasis
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

    <Calculation(Id:="4896c518cce34965a4e1cd7ab38708df")>
    <CalculationSpecification(Id:="P119_InYearLACSubtotal", Name:="P119_InYearLACSubtotal")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="3e4d786c0a984c6c9eb92a959ef1e5f5", Name:="LAC")>
    Public Function P119_InYearLACSubtotal As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim FundingBasis As String = F900_FundingBasis
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

    <Calculation(Id:="da63f0048e914d36ac4ba74677c1ea16")>
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

    <Calculation(Id:="59b09f2f3b2f439dbca50657ea6ae662")>
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

    <Calculation(Id:="0987c37322a24325afcae452a93c448c")>
    <CalculationSpecification(Id:="P177_MobPriSubtotal", Name:="P177_MobPriSubtotal")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="41e8854d7b7e4ecdb46bbc643a62d19e", Name:="Mobility")>
    Public Function P177_MobPriSubtotal As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim SBS_P22 As Decimal = NOR_P22_Total_NOR_PRI_SBS
        Dim APT_ISB_Mobility_Primary As Decimal = Datasets.APTNewISBdataset.MobilityP
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

    <Calculation(Id:="5956a61d6db34744885e13f51c4988b5")>
    <CalculationSpecification(Id:="P178_NSENMobPri", Name:="P178_NSENMobPri")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="41e8854d7b7e4ecdb46bbc643a62d19e", Name:="Mobility")>
    Public Function P178_NSENMobPri As Decimal
        Dim result As Decimal = 0 'change to As String if text product
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

    <Calculation(Id:="83c37040b4554787b82cc25dcd08f345")>
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

    <Calculation(Id:="9843dd7ae88047d699d987987723b358")>
    <CalculationSpecification(Id:="P179_InYearMobPriSubtotal", Name:="P179_InYearMobPriSubtotal")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="41e8854d7b7e4ecdb46bbc643a62d19e", Name:="Mobility")>
    Public Function P179_InYearMobPriSubtotal As Decimal
        Dim result As Decimal = 0 'change to As String if text product
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

    <Calculation(Id:="a59d0c54adfa48c980fc2be305d334e8")>
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

    <Calculation(Id:="0a6727cb41244ebd9296e6867e3a596d")>
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

    <Calculation(Id:="2cfc2c0af2d44a96ad0645b90416dd57")>
    <CalculationSpecification(Id:="P183_MobSecSubtotal", Name:="P183_MobSecSubtotal")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="41e8854d7b7e4ecdb46bbc643a62d19e", Name:="Mobility")>
    Public Function P183_MobSecSubtotal As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim SBS_P25 As Decimal = NOR_P25_Total_NOR_SEC_SBS
        Dim APT_ISB_Mobility_Secondary As Decimal = Datasets.APTNewISBdataset.MobilityS
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

    <Calculation(Id:="61e5bb2ad8c543ac976891bf1e776d3c")>
    <CalculationSpecification(Id:="P184_NSENMobSec", Name:="P184_NSENMobSec")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="41e8854d7b7e4ecdb46bbc643a62d19e", Name:="Mobility")>
    Public Function P184_NSENMobSec As Decimal
        Dim result As Decimal = 0 'change to As String if text product
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

    <Calculation(Id:="355a6bac2c6047378013bb0bd5e26210")>
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

    <Calculation(Id:="7830734bad8f476b9fc2969f9b0e6433")>
    <CalculationSpecification(Id:="P185_InYearMobSecSubtotal", Name:="P185_InYearMobSecSubtotal")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="41e8854d7b7e4ecdb46bbc643a62d19e", Name:="Mobility")>
    Public Function P185_InYearMobSecSubtotal As Decimal
        Dim result As Decimal = 0 'change to As String if text product
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

    <Calculation(Id:="78b724fc9d3744c9aca29f884726b575")>
    <CalculationSpecification(Id:="P239_PriLumpSumFactor", Name:="P239_PriLumpSumFactor")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="41080d8da69046e3a26ff4fb6894eaa8", Name:="Other Factors")>
    Public Function P239_PriLumpSumFactor As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim AcadFilter As Decimal = F200_SBS_Academies
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

    <Calculation(Id:="1fdf234a749240f3a2e7fd82fc347e24")>
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

    <Calculation(Id:="10d8dece03d349efadc6d0028e30cdbf")>
    <CalculationSpecification(Id:="P241_Primary_Lump_Sum", Name:="P241_Primary_Lump_Sum")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="41080d8da69046e3a26ff4fb6894eaa8", Name:="Other Factors")>
    Public Function P241_Primary_Lump_Sum As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="9375cc3ae4564434ba79268e0a66df6d")>
    <CalculationSpecification(Id:="P242_InYearPriLumpSumSubtotal", Name:="P242_InYearPriLumpSumSubtotal")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="41080d8da69046e3a26ff4fb6894eaa8", Name:="Other Factors")>
    Public Function P242_InYearPriLumpSumSubtotal As Decimal
        Dim result As Decimal = 0 'change to As String if text product
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

    <Calculation(Id:="e272b547bba3484eb6fd130cef91df43")>
    <CalculationSpecification(Id:="P243_SecLumpSumFactor", Name:="P243_SecLumpSumFactor")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="41080d8da69046e3a26ff4fb6894eaa8", Name:="Other Factors")>
    Public Function P243_SecLumpSumFactor As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim AcadFilter As Decimal = F200_SBS_Academies
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

    <Calculation(Id:="bb22c0d7b1194b9b913c82bd4e038deb")>
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

    <Calculation(Id:="35e627d7fc7f43fdb55384753b10031a")>
    <CalculationSpecification(Id:="P245_Secondary_Lump_Sum", Name:="P245_Secondary_Lump_Sum")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="41080d8da69046e3a26ff4fb6894eaa8", Name:="Other Factors")>
    Public Function P245_Secondary_Lump_Sum As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="f1c09ce7868c4ffa97cdd57962f1ea18")>
    <CalculationSpecification(Id:="P246_In YearSecLumpSumSubtotal", Name:="P246_In YearSecLumpSumSubtotal")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="41080d8da69046e3a26ff4fb6894eaa8", Name:="Other Factors")>
    Public Function P246_InYearSecLumpSumSubtotal As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="f3362704fe3646308b891a1ffa790cc6")>
    <CalculationSpecification(Id:="P247_NSENLumpSumPri", Name:="P247_NSENLumpSumPri")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="41080d8da69046e3a26ff4fb6894eaa8", Name:="Other Factors")>
    Public Function P247_NSENLumpSumPri As Decimal
        Dim result As Decimal = 0 'change to As String if text product
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

    <Calculation(Id:="4ea50d64d77d4f45991ae8958e931ae0")>
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

    <Calculation(Id:="e79b730a64a44f2f9f7cddd8f0641843")>
    <CalculationSpecification(Id:="P248_NSENLumpSumSec", Name:="P248_NSENLumpSumSec")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="41080d8da69046e3a26ff4fb6894eaa8", Name:="Other Factors")>
    Public Function P248_NSENLumpSumSec As Decimal
        Dim result As Decimal = 0 'change to As String if text product
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

    <Calculation(Id:="7032cd32f05d49f4b79accd346f4b2e7")>
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

    <Calculation(Id:="0d7caee9958c4a41bb0c14b919de69b5")>
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

    <Calculation(Id:="99baeb9cfd1e416db62f55c65f94813c")>
    <CalculationSpecification(Id:="P253_NSENPFI", Name:="P253_NSENPFI")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="41080d8da69046e3a26ff4fb6894eaa8", Name:="Other Factors")>
    Public Function P253_NSENPFI As Decimal
        Dim result As Decimal = 0 'change to As String if text product
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

    <Calculation(Id:="ae5b6bc4dfb34ba8bae21aac259dd639")>
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

    <Calculation(Id:="15e28b2f34cf4d43b6e8f6a0d561e8c9")>
    <CalculationSpecification(Id:="P254_InYearPFISubtotal", Name:="P254_InYearPFISubtotal")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="41080d8da69046e3a26ff4fb6894eaa8", Name:="Other Factors")>
    Public Function P254_InYearPFISubtotal As Decimal
        Dim result As Decimal = 0 'change to As String if text product
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

    <Calculation(Id:="fd63d4deb28348e69c7b78bf4fcda6aa")>
    <CalculationSpecification(Id:="P255_FringeSubtotal", Name:="P255_FringeSubtotal")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="41080d8da69046e3a26ff4fb6894eaa8", Name:="Other Factors")>
    Public Function P255_FringeSubtotal As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim APT_ISB_LondonFringe As Decimal = Datasets.APTNewISBdataset.LondonFringe
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

    <Calculation(Id:="a3b536ff2a13473e8eefa311c09ee8b2")>
    <CalculationSpecification(Id:="P257_InYearFringeSubtotal", Name:="P257_InYearFringeSubtotal")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="41080d8da69046e3a26ff4fb6894eaa8", Name:="Other Factors")>
    Public Function P257_InYearFringeSubtotal As Decimal
        Dim result As Decimal = 0 'change to As String if text product
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

    <Calculation(Id:="c6a0bf7fa93340699d6e20c35a219456")>
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

    <Calculation(Id:="2a9efe9277d9450ea6cefab698f91c33")>
    <CalculationSpecification(Id:="P262_NSENEx1", Name:="P262_NSENEx1")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="41080d8da69046e3a26ff4fb6894eaa8", Name:="Other Factors")>
    Public Function P262_NSENEx1 As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim AcadFilter As Decimal = F200_SBS_Academies
        Dim P248a_NSENLumpSumSec_Percent As Decimal = P248a_NSENLumpSumSec_Percent / 100
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

    <Calculation(Id:="82c1e5e5cbec40f6a20d2d2feb45a808")>
    <CalculationSpecification(Id:="P262a_NSENEx1_Percent", Name:="P262a_NSENEx1_Percent")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="41080d8da69046e3a26ff4fb6894eaa8", Name:="Other Factors")>
    Public Function P262a_NSENEx1_Percent As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="01444512a932458aa97a2af1d3dffd1a")>
    <CalculationSpecification(Id:="P264_InYearEx1Subtotal", Name:="P264_InYearEx1Subtotal")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="41080d8da69046e3a26ff4fb6894eaa8", Name:="Other Factors")>
    Public Function P264_InYearEx1Subtotal As Decimal
        Dim result As Decimal = 0 'change to As String if text product
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

    <Calculation(Id:="7d33681cac4046fbba120499ad2975dc")>
    <CalculationSpecification(Id:="P265_Ex2Subtotal", Name:="P265_Ex2Subtotal")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="41080d8da69046e3a26ff4fb6894eaa8", Name:="Other Factors")>
    Public Function P265_Ex2Subtotal As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim AcadFilter As Decimal = F200_SBS_Academies
        Dim Ex2String As String =
         [Datasets.APTNewISBdataset._1718ApprovedExceptionalCircumstance2  : _Reserved_for_additional_sparsity_lump_sum ] 
        Dim ISB_Ex2 As Decimal =
         [Datasets.APTNewISBdataset._1718ApprovedExceptionalCircumstance2  : _Reserved_for_additional_sparsity_lump_sum ] 
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

    <Calculation(Id:="9bcc8af76c664c83832ead22dbaf5d9c")>
    <CalculationSpecification(Id:="P266_NSENEx2", Name:="P266_NSENEx2")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="41080d8da69046e3a26ff4fb6894eaa8", Name:="Other Factors")>
    Public Function P266_NSENEx2 As Decimal
        Dim result As Decimal = 0 'change to As String if text product
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

    <Calculation(Id:="ffa96ea6840e44578656b0859e67664d")>
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

    <Calculation(Id:="6b3cf45f2c7c4542958f6c4802d75e7b")>
    <CalculationSpecification(Id:="P267_InYearEx2Subtotal", Name:="P267_InYearEx2Subtotal")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="41080d8da69046e3a26ff4fb6894eaa8", Name:="Other Factors")>
    Public Function P267_InYearEx2Subtotal As Decimal
        Dim result As Decimal = 0 'change to As String if text product
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

    <Calculation(Id:="e97d37fdd05749da9f5537e35c6d8c45")>
    <CalculationSpecification(Id:="P269_Ex3Subtotal", Name:="P269_Ex3Subtotal")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="41080d8da69046e3a26ff4fb6894eaa8", Name:="Other Factors")>
    Public Function P269_Ex3Subtotal As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim AcadFilter As Decimal = F200_SBS_Academies
        Dim Ex3String As Decimal = Datasets.APTNewISBdataset._1718ApprovedExceptionalCircumstance3
        Dim ISB_Ex3 As Decimal = Datasets.APTNewISBdataset._1718ApprovedExceptionalCircumstance3
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

    <Calculation(Id:="f48c6a2f9364462198cbe09cb794399a")>
    <CalculationSpecification(Id:="P270_NSENEx3", Name:="P270_NSENEx3")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="41080d8da69046e3a26ff4fb6894eaa8", Name:="Other Factors")>
    Public Function P270_NSENEx3 As Decimal
        Dim result As Decimal = 0 'change to As String if text product
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

    <Calculation(Id:="c32d95fdcfb14909a624b23b8a59d431")>
    <CalculationSpecification(Id:="P270a_NSENEx3_Percent", Name:="P270a_NSENEx3_Percent")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="41080d8da69046e3a26ff4fb6894eaa8", Name:="Other Factors")>
    Public Function P270a_NSENEx3_Percent As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="183ee0fdd6664f3393e2d763ae5e8876")>
    <CalculationSpecification(Id:="P271_InYearEx3Subtotal", Name:="P271_InYearEx3Subtotal")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="41080d8da69046e3a26ff4fb6894eaa8", Name:="Other Factors")>
    Public Function P271_InYearEx3Subtotal As Decimal
        Dim result As Decimal = 0 'change to As String if text product
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

    <Calculation(Id:="bdc0d6f54e76416890c8c77b5c2beaa0")>
    <CalculationSpecification(Id:="P273_Ex4Subtotal", Name:="P273_Ex4Subtotal")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="41080d8da69046e3a26ff4fb6894eaa8", Name:="Other Factors")>
    Public Function P273_Ex4Subtotal As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim AcadFilter As Decimal = F200_SBS_Academies
        Dim Ex4String As Decimal = Datasets.APTNewISBdataset._1718ApprovedExceptionalCircumstance4
        Dim ISB_Ex4 As Decimal = Datasets.APTNewISBdataset._1718ApprovedExceptionalCircumstance4
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

    <Calculation(Id:="2f1e6c7d27f949ada8a9e59307a57e3a")>
    <CalculationSpecification(Id:="P274_NSENEx4", Name:="P274_NSENEx4")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="41080d8da69046e3a26ff4fb6894eaa8", Name:="Other Factors")>
    Public Function P274_NSENEx4 As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim AcadFilter As Decimal = F200_SBS_Academies
        Dim Ex4String As String = Datasets.APTNewISBdataset._1718ApprovedExceptionalCircumstance4
        Dim ISB_Ex4 As Decimal = Datasets.APTNewISBdataset._1718ApprovedExceptionalCircumstance4
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

    <Calculation(Id:="489dce102f39480e9a77c37936e12db4")>
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

    <Calculation(Id:="197ade5d745b4284b1195e031b58a2d9")>
    <CalculationSpecification(Id:="P275_InYearEx4Subtotal", Name:="P275_InYearEx4Subtotal")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="41080d8da69046e3a26ff4fb6894eaa8", Name:="Other Factors")>
    Public Function P275_InYearEx4Subtotal As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="d01220fd76e3485290fb6732e1566020")>
    <CalculationSpecification(Id:="P277_Ex5Subtotal", Name:="P277_Ex5Subtotal")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="41080d8da69046e3a26ff4fb6894eaa8", Name:="Other Factors")>
    Public Function P277_Ex5Subtotal As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim AcadFilter As Decimal = F200_SBS_Academies
        Dim Ex5String As String = Datasets.APTNewISBdataset._1718ApprovedExceptionalCircumstance5
        Dim ISB_Ex5 As Decimal = Datasets.APTNewISBdataset._1718ApprovedExceptionalCircumstance5
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

    <Calculation(Id:="daf12bf289a44fc78154e3a02779f647")>
    <CalculationSpecification(Id:="P278_NSENEx5", Name:="P278_NSENEx5")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="41080d8da69046e3a26ff4fb6894eaa8", Name:="Other Factors")>
    Public Function P278_NSENEx5 As Decimal
        Dim result As Decimal = 0 'change to As String if text product
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

    <Calculation(Id:="a8e2180e24484c579f4e87e1abd12960")>
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

    <Calculation(Id:="34dc593cc74d4848b7ee5b64a95571d2")>
    <CalculationSpecification(Id:="P279_InYearEx5Subtotal", Name:="P279_InYearEx5Subtotal")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="41080d8da69046e3a26ff4fb6894eaa8", Name:="Other Factors")>
    Public Function P279_InYearEx5Subtotal As Decimal
        Dim result As Decimal = 0 'change to As String if text product
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

    <Calculation(Id:="cad416450c284dd88c9ad14b2ba6c343")>
    <CalculationSpecification(Id:="P281_Ex6Subtotal", Name:="P281_Ex6Subtotal")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="41080d8da69046e3a26ff4fb6894eaa8", Name:="Other Factors")>
    Public Function P281_Ex6Subtotal As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim AcadFilter As Decimal = F200_SBS_Academies
        Dim Ex6String As String = Datasets.APTNewISBdataset._1718ApprovedExceptionalCircumstance6
        Dim ISB_Ex6 As Decimal = Datasets.APTNewISBdataset._1718ApprovedExceptionalCircumstance6
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

    <Calculation(Id:="5b7e33d5967c4e21ad5593220d7086b7")>
    <CalculationSpecification(Id:="P282_NSENEx6", Name:="P282_NSENEx6")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="41080d8da69046e3a26ff4fb6894eaa8", Name:="Other Factors")>
    Public Function P282_NSENEx6 As Decimal
        Dim result As Decimal = 0 'change to As String if text product
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

    <Calculation(Id:="6c374724a4834307974f8699b3cb3008")>
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

    <Calculation(Id:="d85ce0a34b4847febc77134ad63458a7")>
    <CalculationSpecification(Id:="P283_InYearEx6Subtotal", Name:="P283_InYearEx6Subtotal")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="41080d8da69046e3a26ff4fb6894eaa8", Name:="Other Factors")>
    Public Function P283_InYearEx6Subtotal As Decimal
        Dim result As Decimal = 0 'change to As String if text product
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

    <Calculation(Id:="5515dcc401674c1fb061d7ed0efff89d")>
    <CalculationSpecification(Id:="P284_NSENSubtotal", Name:="P284_NSENSubtotal")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="41080d8da69046e3a26ff4fb6894eaa8", Name:="Other Factors")>
    Public Function P284_NSENSubtotal As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim APT_NSENSubtotal As Decimal = Datasets.APTNewISBdataset.NotionalSENBudget
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

    <Calculation(Id:="1ce8ea95ee0b4747b4faf99cbd1814f8")>
    <CalculationSpecification(Id:="P285_InYearNSENSubtotal", Name:="P285_InYearNSENSubtotal")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="41080d8da69046e3a26ff4fb6894eaa8", Name:="Other Factors")>
    Public Function P285_InYearNSENSubtotal As Decimal
        Dim result As Decimal = 0 'change to As String if text product
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

    <Calculation(Id:="851c6a53080c4c9baf85d0396f2ebc74")>
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

    <Calculation(Id:="36eb9e6d4af84c7c996d0cb4b8eabe49")>
    <CalculationSpecification(Id:="P287_InYearPriorYearAdjsutmentSubtotal", Name:="P287_InYearPriorYearAdjsutmentSubtotal")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="41080d8da69046e3a26ff4fb6894eaa8", Name:="Other Factors")>
    Public Function P287_InYearPriorYearAdjsutmentSubtotal As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="a18c64b8f3744a849408c2baac11dedb")>
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

    <Calculation(Id:="3645dfa3225a49719a05370118fcc4f7")>
    <CalculationSpecification(Id:="P299_InYearGrowth", Name:="P299_InYearGrowth")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="41080d8da69046e3a26ff4fb6894eaa8", Name:="Other Factors")>
    Public Function P299_InYearGrowth As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim P298Growth As Decimal = P298_Growth
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

    <Calculation(Id:="c5ebb1c61816465194c5d63f9030c59c")>
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

    <Calculation(Id:="b2e722787f2446a6a6d3dd9a3f0b1e26")>
    <CalculationSpecification(Id:="P301_InYearSBSOutcomeAdjustment", Name:="P301_InYearSBSOutcomeAdjustment")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="41080d8da69046e3a26ff4fb6894eaa8", Name:="Other Factors")>
    Public Function P301_InYearSBSOutcomeAdjustment As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim P300SBSAdj As Decimal = P300_SBSOutcomeAdjustment
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

    <Calculation(Id:="ecd8600ed9ea4e75bb543e4ed2bbce41")>
    <CalculationSpecification(Id:="P120_PPAindicator", Name:="P120_PPAindicator")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="c6b57f57424c494fb0ed394ffb1af34c", Name:="Prior Attainment")>
    Public Function P120_PPAindicator As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="f68cf771cc8f4fdf8da98199343a9076")>
    <CalculationSpecification(Id:="P121_PPAY5to6Proportion73", Name:="P121_PPAY5to6Proportion73")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="c6b57f57424c494fb0ed394ffb1af34c", Name:="Prior Attainment")>
    Public Function P121_PPAY5to6Proportion73 As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim FundingBasis As String = F900_FundingBasis
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

    <Calculation(Id:="56ae3d7bf5ce49b4b8e97fb67fbd1263")>
    <CalculationSpecification(Id:="P122_PPAY5to6Proportion78", Name:="P122_PPAY5to6Proportion78")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="c6b57f57424c494fb0ed394ffb1af34c", Name:="Prior Attainment")>
    Public Function P122_PPAY5to6Proportion78 As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim FundingBasis As String = F900_FundingBasis
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

    <Calculation(Id:="d384f1bbfe5c4f71a5fa6dbb7f14b099")>
    <CalculationSpecification(Id:="P122a_PPAY7378forFAPOnly", Name:="P122a_PPAY7378forFAPOnly")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="c6b57f57424c494fb0ed394ffb1af34c", Name:="Prior Attainment")>
    Public Function P122a_PPAY7378forFAPOnly As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="2283d82dd2d844ccaad3c79df0d3f51f")>
    <CalculationSpecification(Id:="P123_PPAY1to4ProportionUnder", Name:="P123_PPAY1to4ProportionUnder")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="c6b57f57424c494fb0ed394ffb1af34c", Name:="Prior Attainment")>
    Public Function P123_PPAY1to4ProportionUnder As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim FundingBasis As String = F900_FUndingBasis
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

    <Calculation(Id:="0950572a33d54042a616db278575ab84")>
    <CalculationSpecification(Id:="P124_PPAY5to6NOR", Name:="P124_PPAY5to6NOR")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="c6b57f57424c494fb0ed394ffb1af34c", Name:="Prior Attainment")>
    Public Function P124_PPAY5to6NOR As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim FundingBasis As Decimal = F900_FundingBasis
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

    <Calculation(Id:="b89be2d4f9ba4397a320be368288f5c8")>
    <CalculationSpecification(Id:="P125_PPAY1to4NOR", Name:="P125_PPAY1to4NOR")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="c6b57f57424c494fb0ed394ffb1af34c", Name:="Prior Attainment")>
    Public Function P125_PPAY1to4NOR As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim FundingBasis As Decimal = F900_FUndingBasis
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

    <Calculation(Id:="a376479853244c4f88fffe38bcc2da0d")>
    <CalculationSpecification(Id:="P126_PPAPriNOR", Name:="P126_PPAPriNOR")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="c6b57f57424c494fb0ed394ffb1af34c", Name:="Prior Attainment")>
    Public Function P126_PPAPriNOR As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim FundingBasis As decimal = F900_FundingBasis
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

    <Calculation(Id:="3f0c0c079eba46e7a79f6f6a83a9e082")>
    <CalculationSpecification(Id:="P127_PPARate", Name:="P127_PPARate")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="c6b57f57424c494fb0ed394ffb1af34c", Name:="Prior Attainment")>
    Public Function P127_PPARate As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim FundingBasis As Decimal = F900_FUndingBasis
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

    <Calculation(Id:="a8460e188a1f4710815c05d92e2f7b9e")>
    <CalculationSpecification(Id:="P128_PPAWeighting", Name:="P128_PPAWeighting")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="c6b57f57424c494fb0ed394ffb1af34c", Name:="Prior Attainment")>
    Public Function P128_PPAWeighting As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim FundingBasis As Decimal = F900_FundingBasis
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

    <Calculation(Id:="1bd6edee23e54d63a6ae5e0a1cbb0d73")>
    <CalculationSpecification(Id:="P129_PPAPupilsY5to6NotAchieving", Name:="P129_PPAPupilsY5to6NotAchieving")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="c6b57f57424c494fb0ed394ffb1af34c", Name:="Prior Attainment")>
    Public Function P129_PPAPupilsY5to6NotAchieving As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim FundingBasis As Decimal = F900_FundingBasis
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

    <Calculation(Id:="06469d17d13b41deab8f37a07285888c")>
    <CalculationSpecification(Id:="P130_PPAPupilsY1to4NotAchieving", Name:="P130_PPAPupilsY1to4NotAchieving")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="c6b57f57424c494fb0ed394ffb1af34c", Name:="Prior Attainment")>
    Public Function P130_PPAPupilsY1to4NotAchieving As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim FundingBasis As Decimal = F900_FundingBasis
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

    <Calculation(Id:="810b35bb8f5f4ff28658c23282869dc7")>
    <CalculationSpecification(Id:="P131_PPATotalPupilsY1to6NotAchieving", Name:="P131_PPATotalPupilsY1to6NotAchieving")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="c6b57f57424c494fb0ed394ffb1af34c", Name:="Prior Attainment")>
    Public Function P131_PPATotalPupilsY1to6NotAchieving As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim FundingBasis As Decimal = F900_FundingBasis
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

    <Calculation(Id:="438970196ab5499587d04bb59ebbe253")>
    <CalculationSpecification(Id:="P132_PPATotalProportionNotAchieving", Name:="P132_PPATotalProportionNotAchieving")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="c6b57f57424c494fb0ed394ffb1af34c", Name:="Prior Attainment")>
    Public Function P132_PPATotalProportionNotAchieving As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim FundingBasis As Decimal = F900_FundingBasis
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

    <Calculation(Id:="9215aab95fca4a6ea21dbd799148c4b5")>
    <CalculationSpecification(Id:="P133_PPATotalFunding", Name:="P133_PPATotalFunding")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="c6b57f57424c494fb0ed394ffb1af34c", Name:="Prior Attainment")>
    Public Function P133_PPATotalFunding As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim FundingBasis As Decimal = F900_FundingBasis
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

    <Calculation(Id:="bda03b5fc4d448d6a676e898c6376e24")>
    <CalculationSpecification(Id:="P134_NSENPPA", Name:="P134_NSENPPA")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="c6b57f57424c494fb0ed394ffb1af34c", Name:="Prior Attainment")>
    Public Function P134_NSENPPA As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim FundingBasis As Decimal = F900_FundingBasis
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

    <Calculation(Id:="4b311d3f675940f1b2d805ba8c1ceab9")>
    <CalculationSpecification(Id:="P134a_NSENPPA_Percent", Name:="P134a_NSENPPA_Percent")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="c6b57f57424c494fb0ed394ffb1af34c", Name:="Prior Attainment")>
    Public Function P134a_NSENPPA_Percent As Decimal
        Dim result As Decimal = 0 'change to As String if text product
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

    <Calculation(Id:="000eea9ee3c84e7797f5d0866af2031a")>
    <CalculationSpecification(Id:="P135_InYearPPASubtotal", Name:="P135_InYearPPASubtotal")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="c6b57f57424c494fb0ed394ffb1af34c", Name:="Prior Attainment")>
    Public Function P135_InYearPPASubtotal As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim FundingBasis As Decimal = F900_FundingBasis
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

    <Calculation(Id:="578ef2824b4d4e75b518d7540a54281b")>
    <CalculationSpecification(Id:="P136_SecPA_Y7Factor", Name:="P136_SecPA_Y7Factor")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="c6b57f57424c494fb0ed394ffb1af34c", Name:="Prior Attainment")>
    Public Function P136_SecPA_Y7Factor As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="1eb73965e38845919c663cc1aa1e924e")>
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

    <Calculation(Id:="f0b783ebc7704534b91f0cb14104305c")>
    <CalculationSpecification(Id:="P138_SecPARate", Name:="P138_SecPARate")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="c6b57f57424c494fb0ed394ffb1af34c", Name:="Prior Attainment")>
    Public Function P138_SecPARate As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim FundingBasis As Decimal = F900_FundingBasis
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

    <Calculation(Id:="e649ac17e4ce4fe2a2069650c0b49034")>
    <CalculationSpecification(Id:="P138a_SecPA_AdjustedSecFactor", Name:="P138a_SecPA_AdjustedSecFactor")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="c6b57f57424c494fb0ed394ffb1af34c", Name:="Prior Attainment")>
    Public Function P138a_SecPA_AdjustedSecFactor As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim F200 As Decimal = F200_SBS_Academies
        Dim P08a_Y7 As Decimal = NOR_P08a_Y7
        Dim P08b_Y8to11 As Decimal = NOR_P08b_Y8to11
        Dim P136_SecPA_Y7Factor As Decimal = P136a_SecPA_Y7NationalWeight()
        If F200 = 1 then
            result =((P136_SecPA_Y7Factor * P08a_Y7 * P136a_SecPA_Y7NationalWeight) + (P137_SecPA_Y8to11Factor * P08b_Y8to11)) / (P08a_Y7 + P08b_Y8to11)
        Else
            exclude(rid)
        End if

        Return result
    End Function

    <Calculation(Id:="96d7655ed18242dbbf93fbd368c7382d")>
    <CalculationSpecification(Id:="P139_SecPASubtotal", Name:="P139_SecPASubtotal")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="c6b57f57424c494fb0ed394ffb1af34c", Name:="Prior Attainment")>
    Public Function P139_SecPASubtotal As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim FundingBasis As Decimal = F900_FundingBasis
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

    <Calculation(Id:="9377fea9e293430f8a95f49f7771fa50")>
    <CalculationSpecification(Id:="P140_NSENSecPA", Name:="P140_NSENSecPA")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="c6b57f57424c494fb0ed394ffb1af34c", Name:="Prior Attainment")>
    Public Function P140_NSENSecPA As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim FundingBasis As Decimal = F900_FundingBasis
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

    <Calculation(Id:="91c164a0696249f9b87ae1694f6b3569")>
    <CalculationSpecification(Id:="P140a_NSENSecPA_Percent", Name:="P140a_NSENSecPA_Percent")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="c6b57f57424c494fb0ed394ffb1af34c", Name:="Prior Attainment")>
    Public Function P140a_NSENSecPA_Percent As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim FundingBasis As Decimal = F900_FundingBasis
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

    <Calculation(Id:="a5f73df774ee4520afc2ac389db560d9")>
    <CalculationSpecification(Id:="P141_InYearSecPASubtotal", Name:="P141_InYearSecPASubtotal")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="c6b57f57424c494fb0ed394ffb1af34c", Name:="Prior Attainment")>
    Public Function P141_InYearSecPASubtotal As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim FundingBasis As Decimal = F900_FundingBasis
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

    <Calculation(Id:="6b3a09aebce74f5691c0eeda31c5effe")>
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

    <Calculation(Id:="8e68ced0e1a043abbc118833fd72006c")>
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

    <Calculation(Id:="5c1f53baf4ac44e5af45aa51175ce8bb")>
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

    <Calculation(Id:="f893de014ba144d98cfad22d065ba3dd")>
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

    <Calculation(Id:="e6cb66ae5d724d5ca34a6aa1e93526e9")>
    <CalculationSpecification(Id:="P189_SparsityTaperFlagAllThru", Name:="P189_SparsityTaperFlagAllThru")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="4eda722dff5b4f6f9e3d13eae928be8a", Name:="Sparsity")>
    Public Function P189_SparsityTaperFlagAllThru As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="ec5a23ff31e8477183bb53282a8907d7")>
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

    <Calculation(Id:="b4e3cfa7829a4520a2e5ab78ae2ec186")>
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

    <Calculation(Id:="753685f914a64170a74abfbfa008f7c8")>
    <CalculationSpecification(Id:="P192_SparsityDistThreshold", Name:="P192_SparsityDistThreshold")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="4eda722dff5b4f6f9e3d13eae928be8a", Name:="Sparsity")>
    Public Function P192_SparsityDistThreshold As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="7d3e1e8358e6451da860eff97c638609")>
    <CalculationSpecification(Id:="P193_SparsityDistMet_YN", Name:="P193_SparsityDistMet_YN")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="4eda722dff5b4f6f9e3d13eae928be8a", Name:="Sparsity")>
    Public Function P193_SparsityDistMet_YN As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="cb754d447859448b8e8315d42c348f31")>
    <CalculationSpecification(Id:="P194_SparsityAveYGSize", Name:="P194_SparsityAveYGSize")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="4eda722dff5b4f6f9e3d13eae928be8a", Name:="Sparsity")>
    Public Function P194_SparsityAveYGSize As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim NOR26 As Decimal = NOR_P26_Total_NOR_SBS
        Dim AcadFilter As Decimal = F200_SBS_Academies
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

    <Calculation(Id:="af6c5f7cc8f5452dad4b26125a45fe3d")>
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

    <Calculation(Id:="527172e7872641a4b76cbd3fa42bede8")>
    <CalculationSpecification(Id:="P196_SparsityYGThresholdMet_YN", Name:="P196_SparsityYGThresholdMet_YN")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="4eda722dff5b4f6f9e3d13eae928be8a", Name:="Sparsity")>
    Public Function P196_SparsityYGThresholdMet_YN As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim AcadFilter As Decimal = F200_SBS_Academies
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

    <Calculation(Id:="2fe37c0587fd424db69ddc240363052d")>
    <CalculationSpecification(Id:="P197_SparsityLumpSumSubtotal", Name:="P197_SparsityLumpSumSubtotal")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="4eda722dff5b4f6f9e3d13eae928be8a", Name:="Sparsity")>
    Public Function P197_SparsityLumpSumSubtotal As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="fe732dd2bcd44abebbc6cf9cf34fad46")>
    <CalculationSpecification(Id:="P198_SparsityTaperSubtotal", Name:="P198_SparsityTaperSubtotal")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="4eda722dff5b4f6f9e3d13eae928be8a", Name:="Sparsity")>
    Public Function P198_SparsityTaperSubtotal As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim Phase As Decimal = P185a_Phase
        Dim P193_SparsityDistMet_YN As Decimal = P193_SparsityDistMet_YN
        Dim P196_SparsityYGThresholdMet_YN As Decimal = P196_SparsityYGThresholdMet_YN
        Dim P190_SparsityUnit As Decimal = P190_SparsityUnit
        Dim APT_ISB_SparsityFunding As Decimal = Datasets.APTNewISBdataset.SparsityFunding
        Dim FundingBasis As Decimal = F900_FundingBasis
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

    <Calculation(Id:="75e83b59a1884ec2b44fcdd9de63ccf2")>
    <CalculationSpecification(Id:="P198a_SubtotalLump_Taper_For_FAP_Only", Name:="P198a_SubtotalLump_Taper_For_FAP_Only")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="4eda722dff5b4f6f9e3d13eae928be8a", Name:="Sparsity")>
    Public Function P198a_SubtotalLump_Taper_For_FAP_Only As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim AcadFilter As Decimal = F200_SBS_Academies
        If AcadFilter = 1 Then
            result = P197_SparsityLumpSumSubtotal + P198_SparsityTaperSubtotal
        Else
            exclude(rid)
        End If

        Print(P198_SparsityTaperSubtotal, "P198_SparsityTaperSubtotal", rid)
        Print(P197_SparsityLumpSumSubtotal, "P197_SparsityLumpSumSubtotal", rid)
        Return result
    End Function

    <Calculation(Id:="170362c015754db5991fbf2a6e55c442")>
    <CalculationSpecification(Id:="P199_InYearSparsityLumpSumSubtotal", Name:="P199_InYearSparsityLumpSumSubtotal")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="4eda722dff5b4f6f9e3d13eae928be8a", Name:="Sparsity")>
    Public Function P199_InYearSparsityLumpSumSubtotal As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="0dd52b40e9884566b8ca32b2b1f9de96")>
    <CalculationSpecification(Id:="P200_InYearSparsityTaperSubtotal", Name:="P200_InYearSparsityTaperSubtotal")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="4eda722dff5b4f6f9e3d13eae928be8a", Name:="Sparsity")>
    Public Function P200_InYearSparsityTaperSubtotal As Decimal
        Dim result As Decimal = 0 'change to As String if text product
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

    <Calculation(Id:="a027521d361c4d9c88f4d0aa5492d7c8")>
    <CalculationSpecification(Id:="P200a_InYear_SubtotalLump_Taper_for_FAP_Only", Name:="P200a_InYear_SubtotalLump_Taper_for_FAP_Only")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="4eda722dff5b4f6f9e3d13eae928be8a", Name:="Sparsity")>
    Public Function P200a_InYear_SubtotalLump_Taper_for_FAP_Only As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="93807b1eae754f49817336ee6268d8ed")>
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

    <Calculation(Id:="9d5dc150df054bfa9b59fb2895a3e620")>
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

    <Calculation(Id:="97d29cd83a794af1b045c78add30cd11")>
    <CalculationSpecification(Id:="P236_NSENSparsity", Name:="P236_NSENSparsity")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="4eda722dff5b4f6f9e3d13eae928be8a", Name:="Sparsity")>
    Public Function P236_NSENSparsity As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim AcadFilter As Decimal = F200_SBS_Academies
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

    <Calculation(Id:="f70f57850562441092a774281324e4b7")>
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

    <Calculation(Id:="24b27747e6494434b13dddb68e51fe6c")>
    <CalculationSpecification(Id:="P249_SplitSiteSubtotal", Name:="P249_SplitSiteSubtotal")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="6857aab8747c4d699b69bba5858b98a3", Name:="Split Sites")>
    Public Function P249_SplitSiteSubtotal As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="571d965f95ab4e3faf2ee83d1f2db1e0")>
    <CalculationSpecification(Id:="P250_NSENSplitSites", Name:="P250_NSENSplitSites")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="6857aab8747c4d699b69bba5858b98a3", Name:="Split Sites")>
    Public Function P250_NSENSplitSites As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim AcadFilter As Decimal = F200_SBS_Academies
        If AcadFilter = 1 then
            Result = P249_SplitSitesSubtotal * P250a_NSENSplitSites_Percent
        Else
            exclude(rid)
        End If

        Print(P249_SplitSitesSubtotal, "P249_SplitSitesSubtotal", rid)
        Print(P250a_NSENSplitSites_Percent, "P250a_NSENSplitSites_Percent", rid)
        Return result
    End Function

    <Calculation(Id:="09f947ca8e6e4866b2ec052bbac076f9")>
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

    <Calculation(Id:="90ec5de9654046f49d001204b5fa4f85")>
    <CalculationSpecification(Id:="P251_InYearSplitSitesSubtotal", Name:="P251_InYearSplitSitesSubtotal")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="6857aab8747c4d699b69bba5858b98a3", Name:="Split Sites")>
    Public Function P251_InYearSplitSitesSubtotal As Decimal
        Dim result As Decimal = 0 'change to As String if text product
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

    <Calculation(Id:="0fe6ce5cdd5c44a0a6671dd3458b9eaa")>
    <CalculationSpecification(Id:="P001_1718DaysOpen", Name:="P001_1718DaysOpen")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="b2e46eab20374cd1864365a94a7ab9b3", Name:="Totals")>
    Public Function P001_1718DaysOpen As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim P001_1718DaysOpen_Local As Decimal = P027_DaysOpen
        Print(P001_1718DaysOpen_Local, "Days Open", rid)
        Print(F200_SBS_Academies, "F200_SBS_Academies", rid)
        If F200_SBS_Academies = 1 Then
            Result = P001_1718DaysOpen_Local
        Else
            Exclude(rid)
        End If

        Return result
    End Function

    <Calculation(Id:="d83728c6a790448f93e508aa8d93fa0c")>
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

    <Calculation(Id:="ed88533502044d9aac7a1c2b42511d29")>
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

    <Calculation(Id:="3ab04d99f3414a678f4b7d2d1badf35d")>
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

    <Calculation(Id:="bd25fbd737b54aceb9d1af3b245d8f17")>
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

    <Calculation(Id:="142dbc5f45914a1a857315760826b1b8")>
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

    <Calculation(Id:="9c76e99f03da4ff6be0755e269531ee0")>
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

    <Calculation(Id:="a9562a3348554a2fb5fe84f9107f42e5")>
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

    <Calculation(Id:="ac83991ff50142eca1963870516453e7")>
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

    <Calculation(Id:="bff72d7756994846a10a2431c6bf9463")>
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

    <Calculation(Id:="a809d60dbb094e27901b6476556bd6ec")>
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

    <Calculation(Id:="d125113a7e7545c0bfb06b39d022cb97")>
    <CalculationSpecification(Id:="P294a_InYearTotalOtherFactors_NoExc", Name:="P294a_InYearTotalOtherFactors_NoExc")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="b2e46eab20374cd1864365a94a7ab9b3", Name:="Totals")>
    Public Function P294a_InYearTotalOtherFactors_NoExc As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="24a3b72d477847bf829a6baf57e98a6c")>
    <CalculationSpecification(Id:="P295_Dedelegation", Name:="P295_Dedelegation")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="b2e46eab20374cd1864365a94a7ab9b3", Name:="Totals")>
    Public Function P295_Dedelegation As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="ab0351cb96e74d7489ba1e5b05057ad4")>
    <CalculationSpecification(Id:="P296_InYearDedelegation", Name:="P296_InYearDedelegation")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="b2e46eab20374cd1864365a94a7ab9b3", Name:="Totals")>
    Public Function P296_InYearDedelegation As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim FundingBasis As Decimal = F900_FundingBasis
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

    <Calculation(Id:="6af48855d80d441abc39d3c926a1325a")>
    Public Function F100_AllAcademies As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim Date_Opened as date = Provider.DateOpened
        Dim Budget_ID1718 as String = Datasets.FundingStreams.Budget(2017181)
        Dim ProviderType as string = Provider.ProviderType
        Dim Funding_Basis1718 as String = Datasets.AcademyInformation.FundingBasis
        Dim SubType As String = Provider.ProviderSubtype
        Dim PrevMain As Boolean = Datasets.AcademyInformation.AcademyPreviouslyMaintained
        Dim ConvertDate As Date = Provider.ConvertDate
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

    <Calculation(Id:="dc7fa18cd6494a5c862083eec828f6b1")>
    Public Function F200_SBS_Academies As Decimal
        Dim result As Decimal = 0 'change to As String if text product
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

    <Calculation(Id:="cc36b9f5408c4121a739b18ab7db91f0")>
    Public Function F300_ESG_Academies_All As Decimal
        Dim result = Decimal.Zero
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

    <Calculation(Id:="26962654ceef4c429573c368d707d3e4")>
    Public Function F301_ESG_Academies_Mainstream As Decimal
        Dim result = Decimal.Zero
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

    <Calculation(Id:="55440ea0b9e9440a85484f3f6f482880")>
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

    <Calculation(Id:="4945698562c54b33a7d034328ff3eca9")>
    Public Function F400_HN_Academies As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        If currentscenario.periodid = 2017181 And (F100_AllAcademies = 17181 Or F100_AllAcademies = 17182 Or F100_AllAcademies = 17183) Then
            Result = 1
        Else
            Exclude(rid)
        End if

        Return result
    End Function

    <Calculation(Id:="dbc59ce7980f48f8b682097585e59913")>
    Public Function F500_MFG_Academies As Decimal
        Dim result As Decimal = 0 'change to As String if text product
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

    <Calculation(Id:="9293670a096742448c132cae3d0b2a97")>
    Public Function F600_ESGProtection_Academies As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Print(F100_AllAcademies, "F100_AllAcademies", rid)
        If(F100_AllAcademies = 17181 Or F100_AllAcademies = 17182 Or F100_AllAcademies = 17183) Then
            Result = 1
        Else
            Result = 0
        End If

        Return result
    End Function

    <Calculation(Id:="9dbddee76dc9409ab4ee5ded4c514190")>
    Public Function F601_ESGProtection_Post16onlyAP As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim F600_ESGProtection_Academies As Decimal = F600_ESGProtection_Academies
        Dim Subtype As String = Provider.ProviderSubtype
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

    <Calculation(Id:="a0967461b5b045d1bfcd3c2626f90fc6")>
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

    <Calculation(Id:="ff5fea155a824deba815560585fb24d9")>
    Public Function F603_ESGProtection_PlaceLed As Decimal
        Dim result As Decimal = 0 'change to As String if text product
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

    <Calculation(Id:="3fb910aaf2b74a93b266d70307484565")>
    Public Function F800_FSProtection_Academies As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim Funding_Basis As string = Datasets.AcademyInformation.FundingBasis
        Dim SubType As String = Provider.ProviderSubtype
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

    <Calculation(Id:="00fc99ec1eca4e40b79095a2c46710b8")>
    Public Function F900_FundingBasis As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim Date_Opened as date = Provider.DateOpened
        Dim Budget_ID as String = Datasets.FundingStreams.Budget(2017181)
        Dim ProviderType as string = Provider.ProviderType
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

    <Calculation(Id:="22bb4a0c91f44c0b9a78c0a9894b6319")>
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

    <Calculation(Id:="f3d01fd234694439b18279b5ad1ae5de")>
    Public Function P001_ESG_MAIN_APP As Decimal
        Dim result = Decimal.Zero
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

    <Calculation(Id:="de46d125e5c54a608647f3c87eebf238")>
    Public Function P010_ESGP_Main_Thresh1 As Decimal
        Dim result = Decimal.Zero
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

    <Calculation(Id:="46746898a2d54b52b4618edd28898ab2")>
    Public Function P011_ESGP_Main_Thresh2 As Decimal
        Dim result = Decimal.Zero
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

    <Calculation(Id:="5752d755ecd54917bfeec89e3a90b77f")>
    Public Function P012_ESGP_AP_Thresh1 As Decimal
        Dim result = Decimal.Zero
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

    <Calculation(Id:="67ffa4277c814c12b4bb0a652e5d2f0a")>
    Public Function P013_ESGP_AP_Thresh2 As Decimal
        Dim result = Decimal.Zero
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

    <Calculation(Id:="b53680c55cee40949add8d0285d86065")>
    Public Function P014_ESGP_Special_Thresh1 As Decimal
        Dim result = Decimal.Zero
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

    <Calculation(Id:="2e769b60d87f41b1b532efbad306198a")>
    Public Function P015_ESGP_Special_Thresh2 As Decimal
        Dim result = Decimal.Zero
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

    <Calculation(Id:="7c0bb48813464a0f853f95003727a298")>
    Public Function P016_MathsTopUp_APP As Decimal
        Dim result = Decimal.Zero
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

    <Calculation(Id:="92085b2a68d54a3fa3bc3328890195f9")>
    Public Function P017_MFG_Level As Decimal
        Dim result = Decimal.Zero
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

    <Calculation(Id:="e7272643b4534cfc82e10b60f270a3f1")>
    Public Function P018_ESG_Main_APP_1617 As Decimal
        Dim result = Decimal.Zero
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

    <Calculation(Id:="cfc4f913d1d2403ba6bbaf5168ce9f3c")>
    Public Function P019_ESGP_Main_Rate_Change As Decimal
        Dim result = Decimal.Zero
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

    <Calculation(Id:="af84ec52c231408fb55f27d54e70c77b")>
    Public Function P002_ESG_AP_APP As Decimal
        Dim result = Decimal.Zero
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

    <Calculation(Id:="d0d71a5a50ae45dbb2afc16cebd0c7c7")>
    Public Function P020_ESGP_Special_Rate_Change As Decimal
        Dim result = Decimal.Zero
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

    <Calculation(Id:="2a9aadc9a1d14977942c34ea3a882735")>
    Public Function P021_ESGP_AP_Rate_Change As Decimal
        Dim result = Decimal.Zero
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

    <Calculation(Id:="b3f279aaffdb47a5b7becee6b604a133")>
    Public Function P022_RPA_Rate As Decimal
        Dim result = Decimal.Zero
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

    <Calculation(Id:="91160eb5444f4411978d38d9f8cf2225")>
    Public Function P023_ESG_HN_APP_1617 As Decimal
        Dim result = Decimal.Zero
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

    <Calculation(Id:="1d9fa8310d7a4190b1bfa2dc273ddc21")>
    Public Function P024_ESG_AP_APP_1617 As Decimal
        Dim result = Decimal.Zero
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

    <Calculation(Id:="0fb266e4166946ad9957251424c5b1aa")>
    Public Function P025_YearDays_1718 As Decimal
        Dim result = Decimal.Zero
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

    <Calculation(Id:="45830d210bd24fd7b5395e083b2f518c")>
    Public Function P026_YearDays_1617 As Decimal
        Dim result = Decimal.Zero
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

    <Calculation(Id:="da2adec7b0c04e16837e92205f217dce")>
    Public Function P027_DaysOpen As Decimal
        Dim result = Decimal.Zero
        Dim Funding_Basis As String = Datasets.AcademyInformation.FundingBasis
        Dim DateOpened As Date = Provider.DateOpened
        Dim ConvertDate As Date = Provider.ConvertDate
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

    <Calculation(Id:="1a27b86c180b4134a896a250793b44c5")>
    Public Function P028_MonthsOpen As Decimal
        Dim result As Decimal = 0
        Dim DateOpened As Date = Provider.DateOpened
        Dim ConvertDate As Date = Provider.ConvertDate
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

    <Calculation(Id:="899fe9c2c00040ce8b43edb29cb88a8a")>
    Public Function P029_FSP_Level As Decimal
        Dim result = Decimal.Zero
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

    <Calculation(Id:="3cd0fd3464ee488ba991e914e96d7180")>
    Public Function P003_ESG_HN_APP As Decimal
        Dim result = Decimal.Zero
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

    <Calculation(Id:="4026d57d4d8f4527a4b46f66562f31d0")>
    Public Function P004_Pre16_HN_APP As Decimal
        Dim result = Decimal.Zero
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

    <Calculation(Id:="bae0b25794854a6bbc6927735f187477")>
    Public Function P005_Pre16_AP_APP As Decimal
        Dim result = Decimal.Zero
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

    <Calculation(Id:="b61daf68dca84769a5130d017176977f")>
    Public Function P006_Post16_HN_APP As Decimal
        Dim result = Decimal.Zero
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

    <Calculation(Id:="0ab9fa44d170408f9d9519343ef5f07b")>
    Public Function P007_ESGP_Cond1 As Decimal
        Dim result = Decimal.Zero
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

    <Calculation(Id:="c7ffaa014b1146899359e9481289e60e")>
    Public Function P008_ESGP_Cond2 As Decimal
        Dim result = Decimal.Zero
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

    <Calculation(Id:="5e4d5f5ec9824186a321d43745a10615")>
    Public Function P009_ESGP_Cond3 As Decimal
        Dim result = Decimal.Zero
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

    <Calculation(Id:="ee486779c3714cfea4b78b676f5fd60d")>
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

    <Calculation(Id:="30bf2321802b4742841df03084900f2e")>
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

    <Calculation(Id:="f1c11ff1cb86487b945a507e78795a37")>
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

    <Calculation(Id:="ff6b6126d2894fa59133243e1e0b7d98")>
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

    <Calculation(Id:="6e909913734f4ec2939da3e107fc7466")>
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

    <Calculation(Id:="d4eac75dd894488faddf3a505bb92fe1")>
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

    <Calculation(Id:="9ab20f7f616547718ed44eb33decc52f")>
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

    <Calculation(Id:="dadaa30c62a4426a95c5ebf3f761e741")>
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

    <Calculation(Id:="a697fe80d65f449f9dea2b3bcb0d3040")>
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

    <Calculation(Id:="c981c4c81c6e44faa8b93bbaa364b94e")>
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

    <Calculation(Id:="a65a7bfa6186482a9ca3e488ce640b00")>
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

    <Calculation(Id:="f85bd9f207c14838912b2187c5881d12")>
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

    <Calculation(Id:="5b7e4abe8bed4e8e97739834cc5bf6a3")>
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

    <Calculation(Id:="db96a15ce40c44d18f1b44a88a6faa1d")>
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

    <Calculation(Id:="e26614346e89460fa8ca36feea826896")>
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

    <Calculation(Id:="78dce8901605497fac69a647573bbb11")>
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

    <Calculation(Id:="b4b809bd63d24eb0864f7de4cfbaec79")>
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

    <Calculation(Id:="73cea0d890554e669f3434a0e542f720")>
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

    <Calculation(Id:="2375df5f790e4308b5d26a1dbec43c91")>
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

    <Calculation(Id:="57b452b3eaca4253ac780d8d5113cc94")>
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

    <Calculation(Id:="18e0154ecf08413eb89ea8fda2d583aa")>
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

    <Calculation(Id:="faf5dbda092c435e88530ae0ba896fc5")>
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

    <Calculation(Id:="43712a61b3384b30ad08856f2106771e")>
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

    <Calculation(Id:="4e128aafc71c4d33a24a2f8d4504db9e")>
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

    <Calculation(Id:="915ef4c72ce94e7ea731deaa2e92a783")>
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

    <Calculation(Id:="e52af6eaa56141ae86ae9c9c066a7f92")>
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

    <Calculation(Id:="a19cd8b4ff9942e48974b0d7c1e72643")>
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

    <Calculation(Id:="8d435fdf307041cdb360af0431e5eff3")>
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

    <Calculation(Id:="af7811c1cd2a4300b1ad04f0aaa87c10")>
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

    <Calculation(Id:="1d138cccea2a4c91b28f82098677f4ce")>
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

    <Calculation(Id:="bfe94b7435c6475fa91425899a94b04f")>
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

    <Calculation(Id:="4d59974ff7c3488781bd608b6ae23d14")>
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

    <Calculation(Id:="fad3818025964ee490963654882a86dc")>
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

    <Calculation(Id:="4c5763ea9c14447fa1bf619b05674fcb")>
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

    <Calculation(Id:="1a229d3b307c40ecb46b969ef8746606")>
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

    <Calculation(Id:="ddc8a6149c944bbbac0c639c5cc82049")>
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

    <Calculation(Id:="db11e6615c2f4238a0b7d4763a1a0e31")>
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

    <Calculation(Id:="4724d01268d84cb39be14d27853ba2d9")>
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

    <Calculation(Id:="c0615b574ceb4d879e323392b699232f")>
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

    <Calculation(Id:="4e3bf4fc5d194025ae63fefdca1c32c3")>
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

    <Calculation(Id:="e637cbcef000422ba8048415b96a3011")>
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

    <Calculation(Id:="455a455129174dbca2cdc31c27079d17")>
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

    <Calculation(Id:="24bf94554abd4ab68c79ab37afab9669")>
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

    <Calculation(Id:="00619ca0c39f402f91ee7b9bb4c23424")>
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

    <Calculation(Id:="98046897833a4fbeb7871ac6255d0701")>
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

    <Calculation(Id:="e55f19845f8a47b68530279502154434")>
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

    <Calculation(Id:="b603eb86228d40c2827cf45d7881db43")>
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

    <Calculation(Id:="383381c8dc804838987ec0136c2d4892")>
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

    <Calculation(Id:="47cadf51401f41c3a9375e7f00a5f0e5")>
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

    <Calculation(Id:="1f87d7cb9fc6459db9f5bed3c93ebc09")>
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

    <Calculation(Id:="2989dd179cd542c08721e6661dba833b")>
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

    <Calculation(Id:="e520354942f143f68b8aaf8cbd3e8c10")>
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

    <Calculation(Id:="409c7760a94d4a109c223468d03f0785")>
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

    <Calculation(Id:="43ddc6bfabfc4a5b8638a1bbf862c8d7")>
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

    <Calculation(Id:="61a7abcbf8d94d639bc21b39caa50f8e")>
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

    <Calculation(Id:="df521259956a4e0c99741fe558a83a82")>
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

    <Calculation(Id:="d188a90740cb4b83a608d04a5b0e251d")>
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

    <Calculation(Id:="bdccfdd298d743928fcad3c75fda266d")>
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

    <Calculation(Id:="81ccbad8f98c4be5967eb5a245f40762")>
    Public Function NOR_P03_Y1Y4 As Decimal
        Dim result = Decimal.Zero
        Dim IsNull As Boolean
        IsNull = IIf(Datasets.APTInputsandAdjustments.NORYr14, false, true)
        Dim DateOpened as date = Provider.DateOpened
        Dim NOR_Y1Y4_Census As Decimal = Datasets.CensusNumberCounts.NORY14
        Dim NOR_Y1Y4_InpAdj As Decimal = Datasets.APTInputsandAdjustments.NORYr14
        Dim NOR_InpAdj As Decimal = Datasets.APTInputsandAdjustments.NOR
        Dim NOR_Y1Y4_Est As Decimal = Datasets.EstimateNumberCounts.EstNORY14
        Dim NOR_Y1Y4_APT As Decimal = Datasets.APTAdjustedFactorsdataset.NOR14forcalculationoftheeligiblepupilsfortheprimarypriorattainmentfactor
        Dim NOR_P008 As double = P008_NOR_Est_RtoY11
        Dim NOR_P002 As double = P002_NOR_Est_Y1to4
        Dim Guaranteed As String = Datasets.APTInputsandAdjustments.Amendedpupilnumbersguaranteed ? 
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

    <Calculation(Id:="86bac971bfd643528e88edfdbbbc910b")>
    Public Function NOR_P04_Y5Y6 As Decimal
        Dim result = Decimal.Zero
        Dim IsNull As Boolean
        IsNull = IIf(Datasets.APTInputsandAdjustments.NORYr56, false, true)
        Dim NOR_Y5Y6_InpAdj As Decimal = Datasets.APTInputsandAdjustments.NORYr56
        Dim DateOpened as date = Provider.DateOpened
        Dim NOR_Y5Y6_Census As Decimal = Datasets.CensusNumberCounts.NORY56
        Dim NOR_P008 As Decimal = P008_NOR_Est_RtoY11
        Dim NOR_P003 As Decimal = P003_NOR_Est_Y5to6
        Dim NOR_Y5Y6_APT As Decimal = Datasets.APTAdjustedFactorsdataset.NOR56forcalculationoftheeligiblepupilsfortheprimarypriorattainmentfactor
        Dim Guaranteed As String = Datasets.APTInputsandAdjustments.Amendedpupilnumbersguaranteed ? 
        Dim NOR_InpAdj As Decimal = Datasets.APTInputsandAdjustments.NOR
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

    <Calculation(Id:="e2658634882847cd922a1f621d157c05")>
    Public Function NOR_P05_NUR As Decimal
        Dim result = Decimal.Zero
        Dim IsNull As Boolean
        Dim NOR_Nursery_Census = Datasets.CensusNumberCounts.FTENursery
        Dim NOR_Nursery_RFDC = Datasets.EstimateNumberCounts.EstFTENursery
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

    <Calculation(Id:="3e74eb4484864a4195018b2fc5d0b317")>
    Public Function NOR_P06_SEC As Decimal
        Dim result = Decimal.Zero
        Dim NOR_P07 As Decimal = NOR_P07_KS3
        Dim NOR_P08 As Decimal = NOR_P08_KS4
        If currentscenario.periodid = 2017181 And (F100_AllAcademies = 17181 Or F100_AllAcademies = 17182 Or F100_AllAcademies = 17183) then
            result = NOR_P07 + NOR_P08
        Else
            Exclude(rid)
        End If

        Print(F900_FundingBasis, "F900_FundingBasis", rid)
        Print(F100_AllAcademies, "F100_AllAcademies", rid)
        Return Result
    End Function

    <Calculation(Id:="00abb6a8523f4c26a42ed189a535bba4")>
    Public Function NOR_P07_KS3 As Decimal
        Dim result = Decimal.Zero
        Dim IsNull As Boolean = iif(Datasets.APTInputsandAdjustments.NORKS3, false, true)
        Dim DateOpened As Date = Provider.DateOpened
        Dim NOR_KS3_InpAdj As Decimal = Datasets.APTInputsandAdjustments.NORKS3
        Dim NOR_InpAdj As Decimal = Datasets.APTInputsandAdjustments.NOR
        Dim NOR_KS3_Cen As Decimal = Datasets.CensusNumberCounts.NORKS3
        Dim NOR_P006 As Decimal = P006_NOR_Est_KS3
        Dim NOR_P008 As Decimal = P008_NOR_Est_RtoY11
        Dim NOR_KS3_AdjFact As Decimal = Datasets.APTAdjustedFactorsdataset.NORKS3
        Dim Guaranteed As String = Datasets.APTInputsandAdjustments.Amendedpupilnumbersguaranteed ? 
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

    <Calculation(Id:="1c8b8e67c24c47f3b1fe62e3bb61b8a7")>
    Public Function NOR_P08_KS4 As Decimal
        Dim result = Decimal.Zero
        Dim DateOpened As Date = Provider.DateOpened
        Dim IsNull As Boolean
        IsNull = IIf(Datasets.APTInputsandAdjustments.NORKS4, false, true)
        Dim NOR_KS4_InpAdj As Decimal = Datasets.APTInputsandAdjustments.NORKS4
        Dim NOR_InpAdj As Decimal = Datasets.APTInputsandAdjustments.NOR
        Dim NOR_KS4_Cen As Decimal = Datasets.CensusNumberCounts.NORKS4
        Dim NOR_P007 As Decimal = P007_NOR_Est_KS4
        Dim NOR_P008 As Decimal = P008_NOR_Est_RtoY11
        Dim NOR_KS4_AdjFact As Decimal = Datasets.APTAdjustedFactorsdataset.NORKS4
        Dim Guaranteed As String = Datasets.APTInputsandAdjustments.Amendedpupilnumbersguaranteed ? 
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

    <Calculation(Id:="5d3de3b0d75044fba037bf93708b6c01")>
    Public Function NOR_P09_APT_HN_PRI As Decimal
        Dim result = Decimal.Zero
        Dim NOR_APT_HN_Pri As Decimal = Datasets.APTLocalfactorsdataset.NumberofprimarypupilsonrollattheschoolinHighNeedsplacesin201617
        Print(NOR_APT_HN_Pri, "APT HN Pri", rid)
        If currentscenario.periodid = 2017181 And (F100_AllAcademies = 17181 Or F100_AllAcademies = 17182 Or F100_AllAcademies = 17183) then
            result = NOR_APT_HN_Pri
        Else
            Exclude(rid)
        End if

        Return result
    End Function

    <Calculation(Id:="82b2966186fc405dbc6f19e74248aa00")>
    Public Function NOR_P10_APT_HN_KS3 As Decimal
        Dim result = Decimal.Zero
        Dim NOR_APT_HN_KS3 As Decimal = Datasets.APTLocalfactorsdataset.NumberofKS3pupilsonrollattheschoolinHighNeedsplacesin201617
        If currentscenario.periodid = 2017181 And (F100_AllAcademies = 17181 Or F100_AllAcademies = 17182 Or F100_AllAcademies = 17183) then
            result = NOR_APT_HN_KS3
        Else
            Exclude(rid)
        End if

        return Result
    End Function

    <Calculation(Id:="8548a3adad79436a851a715abe6acc0b")>
    Public Function NOR_P11_APT_HN_KS4 As Decimal
        Dim result = Decimal.Zero
        Dim NOR_APT_HN_KS4 As Decimal = Datasets.APTLocalfactorsdataset.NumberofKS4pupilsonrollattheschoolinHighNeedsplacesin201617
        Print(NOR_APT_HN_KS4, "APT_HN_HN_KS4", rid)
        If currentscenario.periodid = 2017181 And (F100_AllAcademies = 17181 Or F100_AllAcademies = 17182 Or F100_AllAcademies = 17183) then
            result = NOR_APT_HN_KS4
        Else
            Exclude(rid)
        End if

        return Result
    End Function

    <Calculation(Id:="856988ba4c774984921b66c1986ed424")>
    Public Function NOR_P12_HND_HNP_HN_PRI As Decimal
        Dim result = Decimal.Zero
        Dim HNs_Places_Total As Decimal = Datasets.HighNeedsPlaces.Totalpre16HNsplaces
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

    <Calculation(Id:="d51aac88f2144daba0c78ef4e5708b96")>
    Public Function NOR_P13_HND_HNP_HN_KS3 As Decimal
        Dim result = Decimal.Zero
        Dim HNs_Places_Total As Decimal = Datasets.HighNeedsPlaces.Totalpre16HNsplaces
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

    <Calculation(Id:="62b949374c6a4b82b67441de1e22fb89")>
    Public Function NOR_P14_HND_HNP_HN_KS4 As Decimal
        Dim result = Decimal.Zero
        Dim HNs_Places_Total As Decimal = Datasets.HighNeedsPlaces.Totalpre16HNsplaces
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

    <Calculation(Id:="e30b09a2e2c748ee8a09c2f6ad36f334")>
    Public Function NOR_P15_HND_HNP_AP_PRI As Decimal
        Dim result = Decimal.Zero
        Dim AP_Places_Total As Decimal = Datasets.HighNeedsPlaces.Totalpre16APplaces
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

    <Calculation(Id:="552ce1fdf94c4a5aac1161d2ac99e141")>
    Public Function NOR_P16_HND_HNP_AP_KS3 As Decimal
        Dim result = Decimal.Zero
        Dim AP_Places_Total As Decimal = Datasets.HighNeedsPlaces.Totalpre16APplaces
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

    <Calculation(Id:="a850eff038ef4faba9020e1aae9719c2")>
    Public Function NOR_P17_HND_HNP_AP_KS4 As Decimal
        Dim result = Decimal.Zero
        Dim AP_Places_Total As Decimal = Datasets.HighNeedsPlaces.Totalpre16APplaces
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

    <Calculation(Id:="00a156e73041494da3f262d3e1875724")>
    Public Function NOR_P18_HND_HN_Pre16 As Decimal
        Dim result = Decimal.Zero
        Dim Total_HNs_Places_Pre16 As Decimal = Datasets.HighNeedsPlaces.Totalpre16HNsplaces
        If currentscenario.periodid = 2017181 And (F100_AllAcademies = 17181 Or F100_AllAcademies = 17182 Or F100_AllAcademies = 17183) Then
            Result = Total_HNs_Places_Pre16
        Else
            Exclude(rid)
        End if

        Return result
    End Function

    <Calculation(Id:="d48ec2a6f023454c995648810f9e412c")>
    Public Function NOR_P19_HND_AP_Pre16 As Decimal
        Dim result = Decimal.Zero
        Dim Total_AP_Places_Pre16 As Decimal = Datasets.HighNeedsPlaces.Totalpre16APplaces
        If currentscenario.periodid = 2017181 And (F100_AllAcademies = 17181 Or F100_AllAcademies = 17182 Or F100_AllAcademies = 17183) Then
            Result = Total_AP_Places_Pre16
        Else
            Exclude(rid)
        End If

        Return result
    End Function

    <Calculation(Id:="76f79709d7d44f77a28399066eb4b388")>
    Public Function NOR_P20_HND_Hosp_Pl As Decimal
        Dim result = Decimal.Zero
        Dim NOR_HND_Hosp_Pl As Decimal = Datasets.HighNeedsPlaces.Hospitalprovisionplaces
        If currentscenario.periodid = 2017181 And (F100_AllAcademies = 17181 Or F100_AllAcademies = 17182 Or F100_AllAcademies = 17183) then
            Result = NOR_HND_Hosp_Pl
        Else
            Exclude(rid)
        End If

        Return result
    End Function

    <Calculation(Id:="0800e35726c8459eb11663dafa531fb6")>
    Public Function NOR_P21_P16 As Decimal
        Dim result = Decimal.Zero
        Dim NOR_P21_P16_Local As Decimal = Products.AY1718_Acad_Post16.P03_Learners
        If currentscenario.periodid = 2017181 And (F100_AllAcademies = 17181 Or F100_AllAcademies = 17182 Or F100_AllAcademies = 17183) then
            Result = NOR_P21_P16_Local
        Else
            Exclude(rid)
        End If

        Return result
    End Function

    <Calculation(Id:="f855bb467d7940ff808f7eeeb3dacae0")>
    Public Function NOR_P21b_P16_HN As Decimal
        Dim result = Decimal.Zero
        Dim NOR_P21_P16 As Decimal = Products.AY1718_Acad_Post16.P04_HNPlaces
        If currentscenario.periodid = 2017181 And (F100_AllAcademies = 17181 Or F100_AllAcademies = 17182 Or F100_AllAcademies = 17183) then
            Result = NOR_P21_P16
        Else
            Exclude(rid)
        End If

        Return result
    End Function

    <Calculation(Id:="51d1562f141c449f944dbc1763d60dde")>
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

    <Calculation(Id:="efa33de905ad4233944b8a8e8d9cb699")>
    Public Function NOR_P42a_Year_Groups_Primary As Decimal
        Dim result = Decimal.Zero
        Dim IsNull As Boolean = iif(Datasets.APTInputsandAdjustments.NumberofPrimaryyeargroupsforallschools, false, true)
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

    <Calculation(Id:="4f4886cc65d94f239799b8fde4b03bc5")>
    Public Function NOR_P42b_Year_Groups_Secondary As Decimal
        Dim result = Decimal.Zero
        Dim IsNull = iif(Datasets.APTInputsandAdjustments.NumberofSecondaryyeargroupsforallschools, false, true)
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

    <Calculation(Id:="21c954f89bc146a781e38a2879e7010c")>
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

    <Calculation(Id:="c5dbc5e9fe5240d69d737647b3308c4a")>
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

    <Calculation(Id:="1eaeed4f28ca433999867a1ab20712b9")>
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

    <Calculation(Id:="40024e729e1449788cb2bde4f85586df")>
    Public Function P002_NOR_Est_Y1to4 As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim Y1 As Decimal = Datasets.EstimateNumberCounts.EstNORY1
        Dim Y2 As Decimal = Datasets.EstimateNumberCounts.EstNORY2
        Dim Y3 As Decimal = Datasets.EstimateNumberCounts.EstNORY3
        Dim Y4 As Decimal = Datasets.EstimateNumberCounts.EstNORY4
        result =(Y1 + Y2 + Y3 + Y4)
        Return result
    End Function

    <Calculation(Id:="0adec3bfc4014ad0ba9d01484b1695d8")>
    Public Function P003_NOR_Est_Y5to6 As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim Y5 As Decimal = Datasets.EstimateNumberCounts.EstNORY5
        Dim Y6 As Decimal = Datasets.EstimateNumberCounts.EstNORY6
        Result = Y5 + Y6
        Return result
    End Function

    <Calculation(Id:="ff68de461ce948c5bc1bb141b04481b8")>
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

    <Calculation(Id:="39586586c26a435da01a69241e2af771")>
    Public Function P005_NOR_Est_Y8to11 As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim Y8 As Decimal = Datasets.EstimateNumberCounts.EstNORY8
        Dim Y9 As Decimal = Datasets.EstimateNumberCounts.EstNORY9
        Dim Y10 As Decimal = Datasets.EstimateNumberCounts.EstNORY10
        Dim Y11 As Decimal = Datasets.EstimateNumberCounts.EstNORY11
        result =(Y8 + Y9 + Y10 + Y11)
        Return result
    End Function

    <Calculation(Id:="49b389acf52440779383868b31cb09b6")>
    Public Function P006_NOR_Est_KS3 As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim Y7 As Decimal = Datasets.EstimateNumberCounts.EstNORY7
        Dim Y8 As Decimal = Datasets.EstimateNumberCounts.EstNORY8
        Dim Y9 As Decimal = Datasets.EstimateNumberCounts.EstNORY9
        result =(Y7 + Y8 + Y9)
        Return result
    End Function

    <Calculation(Id:="1df3afc97cc84946a37fa8bfabd7d107")>
    Public Function P007_NOR_Est_KS4 As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim Y10 As Decimal = Datasets.EstimateNumberCounts.EstNORY10
        Dim Y11 As Decimal = Datasets.EstimateNumberCounts.EstNORY11
        result = Y10 + Y11
        Return result
    End Function

    <Calculation(Id:="e050313a6d5a4c31b587ee9db1be80ab")>
    Public Function P008_NOR_Est_RtoY11 As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim Pri As Decimal = P001_NOR_Est_Pri
        Dim Sec As Decimal = P004_NOR_Est_Sec
        result = Pri + Sec
        Return result
    End Function

    <Calculation(Id:="a51fee5afc0f4504aa0a745a148f605d")>
    Public Function P009_NOR_Est_Y12toY14 As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim Y12 As Decimal = Datasets.EstimateNumberCounts.EstNORY12
        Dim Y13 As Decimal = Datasets.EstimateNumberCounts.EstNORY13
        Dim Y14 As Decimal = Datasets.EstimateNumberCounts.EstNORY14
        result = Y12 + Y13 + Y14
        Return result
    End Function

    <Calculation(Id:="d99751234d424de18e0766c374abd504")>
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

    <Calculation(Id:="b44408fafa684f41b38217d896823bc8")>
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

    <Calculation(Id:="02570a81fbef4631b009c181dd7a924f")>
    Public Function P012_NOR_Total_YG_R_Y11 As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim P010 As Decimal = P010_NOR_TotalPri_YG
        Dim P011 As Decimal = P011_NOR_TotalSec_YG
        result = P010 + P011
        Return result
    End Function

    <Calculation(Id:="bf5337cc8e374c5387929d7ad01ede09")>
    Public Function NOR_P23_Total_NOR_KS3_SBS As Decimal
        Dim result = Decimal.Zero
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

    <Calculation(Id:="6ec5783930784c0aa270eae7406b407b")>
    Public Function NOR_P23b_Actual_HN_KS3_Deducted As Decimal
        Dim result = Decimal.Zero
        Dim TotalPlacesAPT As Decimal = Datasets.APTLocalfactorsdataset.TotalnumberofHighNeedsplacesin201617
        Dim APT As Boolean
        APT = IIf(Datasets.APTLocalfactorsdataset.TotalnumberofHighNeedsplacesin201617, false, true)
        Dim NOR_Inp_Adj As Decimal = Datasets.APTInputsandAdjustments.NORKS3
        Dim Pre16HNData As Decimal = Datasets.HighNeedsPlaces.Totalpre16HNsplaces
        Dim Pre16APData As Decimal = Datasets.HighNeedsPlaces.Totalpre16APplaces
        Dim TotalPlacesHNData As Decimal = Pre16HNData + Pre16APData
        Dim NOR_P16_HND_HNP_AP_KS3 As Decimal = NOR_P16_HND_HNP_AP_KS3
        Dim NOR_P13_HND_HNP_HN_KS3 As Decimal = NOR_P13_HND_HNP_HN_KS3
        Dim HND_HN_KS3 As Decimal = NOR_P16_HND_HNP_AP_KS3 + NOR_P13_HND_HNP_HN_KS3
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

    <Calculation(Id:="6166f6cceb1e46269fcf7ee11200de67")>
    Public Function NOR_P24_Total_NOR_KS4_SBS As Decimal
        Dim result = Decimal.Zero
        If currentscenario.periodid = 2017181 And (F100_AllAcademies = 17182 Or F100_AllAcademies = 17183) And F900_FundingBasis = 1 Then
            Result = NOR_P08_KS4
        Else
            Result = NOR_P08_KS4 - NOR_P24b_Actual_HN_KS4_Deducted
        End If

        Return result
    End Function

    <Calculation(Id:="cae122f81e0f43c8ae513abc0bd90da6")>
    Public Function NOR_P24b_Actual_HN_KS4_Deducted As Decimal
        Dim result = Decimal.Zero
        Dim TotalPlacesAPT As Decimal = Datasets.APTLocalfactorsdataset.TotalnumberofHighNeedsplacesin201617
        Dim APT As Boolean
        APT = IIf(Datasets.APTLocalfactorsdataset.TotalnumberofHighNeedsplacesin201617, false, true)
        Dim NOR_Inp_Adj As Decimal = Datasets.APTInputsandAdjustments.NORKS4
        Dim Pre16HNData As Decimal = Datasets.HighNeedsPlaces.Totalpre16HNsplaces
        Dim Pre16APData As Decimal = Datasets.HighNeedsPlaces.Totalpre16APplaces
        Dim TotalPlacesHNData As Decimal = Pre16HNData + Pre16APData
        Dim NOR_P17_HND_HNP_AP_KS4 As Decimal = NOR_P17_HND_HNP_AP_KS4
        Dim NOR_P14_HND_HNP_HN_KS4 As Decimal = NOR_P14_HND_HNP_HN_KS4
        Dim HND_HN_KS4 As Decimal = [NOR_P14_HND_HNP_HN_KS4] + [NOR_P17_HND_HNP_AP_KS4]
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

    <Calculation(Id:="636246bc12104700bdff6ee480aa3ede")>
    Public Function NOR_P25_Total_NOR_SEC_SBS As Decimal
        Dim result = Decimal.Zero
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

    <Calculation(Id:="f269915f6c9b4256bff5a31a06952d94")>
    Public Function NOR_P25b_Actual_HN_Sec_deducted As Decimal
        Dim result = Decimal.Zero
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

    <Calculation(Id:="e2ec3fcc14b042dfb89e45a4a3ddb74f")>
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

    <Calculation(Id:="de1eccf0c7f84219b23cae830483e37e")>
    Public Function NOR_P26b_Total_Actual_HN_deducted As Decimal
        Dim result = Decimal.Zero
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

    <Calculation(Id:="687d921897174a009205e9afa27b9bf6")>
    Public Function NOR_P22_Total_NOR_PRI_SBS As Decimal
        Dim result = Decimal.Zero
        Dim NOR_P02_Pri As Decimal = NOR_P02_PRI
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

    <Calculation(Id:="6edd932d77854db8a4533c163404b889")>
    Public Function NOR_P22b_Actual_HN_Pri_Deducted As Decimal
        Dim result = Decimal.Zero
        Dim TotalPlacesAPT As Decimal = Datasets.APTLocalfactorsdataset.TotalnumberofHighNeedsplacesin201617
        Dim APT As Boolean
        APT = IIf(Datasets.APTLocalfactorsdataset.TotalnumberofHighNeedsplacesin201617, false, true)
        Dim NOR_Inp_Adj As Decimal = Datasets.APTInputsandAdjustments.NORPrimary
        Dim Pre16HNData As Decimal = Datasets.HighNeedsPlaces.Totalpre16HNsplaces
        Dim Pre16APData As Decimal = Datasets.HighNeedsPlaces.Totalpre16APplaces
        Dim TotalPlacesHNData As Decimal = Pre16HNData + Pre16APData
        Dim NOR_P02_Pri As Decimal = NOR_P02_PRI
        Dim NOR_P09_APT_HN_Pri As Decimal = NOR_P09_APT_HN_PRI
        Dim NOR_P15_HND_HNP_AP_Pri As Decimal = NOR_P15_HND_HNP_AP_PRI
        Dim NOR_P12_HND_HNP_HN_Pri As Decimal = NOR_P12_HND_HNP_HN_PRI
        Dim HND_HN_Pri As Decimal = NOR_P12_HND_HNP_HN_Pri + NOR_P15_HND_HNP_AP_Pri
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

    <Calculation(Id:="c969e2db24f54183b6fadcfd98cd0ae5")>
    Public Function NOR_P27a_Total_NOR_MFG As Decimal
        Dim result = Decimal.Zero
        Print(F100_AllAcademies, "F100_AllAcademies", rid)
        If currentscenario.periodid = 2017181 And (F100_AllAcademies = 17181 Or F100_AllAcademies = 17182 Or F100_AllAcademies = 17183) Then
            Result = NOR_P02_PRI + NOR_P06_SEC
        Else
            Exclude(rid)
        End If

        Return result
    End Function

    <Calculation(Id:="efe11231c80945caabcfd1002fe12804")>
    Public Function NOR_P27b_Total_HN_MFG As Decimal
        Dim result = Decimal.Zero
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

    <Calculation(Id:="dd9c840103e041e79bd0f12610a6c30b")>
    Public Function NOR_P27c_total_NOR_MFG_forPupilMatrix As Decimal
        Dim result = Decimal.Zero
        Dim P22_Total_NOR_Pri_SBS As Decimal = NOR_P22_Total_NOR_PRI_SBS
        Dim P25_Total_NOR_Sec_SBS As Decimal = NOR_P25_Total_NOR_SEC_SBS
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

    <Calculation(Id:="cae4fcc17d9147faa3f1d5002d28e55c")>
    Public Function NOR_P28_Total_NOR_Mainstream_ESG As Decimal
        Dim result = Decimal.Zero
        Dim NOR_P05_Nur As Decimal = NOR_P05_NUR
        Dim NOR_P02_Pri As Decimal = NOR_P02_PRI
        Dim NOR_P06_Sec As Decimal = NOR_P06_SEC
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

    <Calculation(Id:="66d57d267949456aa0f1a239cdcea215")>
    Public Function NOR_P29_Total_NOR_HNPlaces_ESG As Decimal
        Dim result = Decimal.Zero
        'ESG does Not exist In 1718 so this has been Set To zero
        'If currentscenario.periodid = 2017181 And (F100_AllAcademies = 17181 Or F100_AllAcademies = 17182 Or F100_AllAcademies = 17183) then    
        '  Result = NOR_P18_HND_HN_Pre16
        'Else Exclude(rid)
        'End If
        result = 0
        Return result
    End Function

    <Calculation(Id:="23a5668de57d4fd4ab2fd03a7ac0ea3b")>
    Public Function NOR_P30_Total_NOR_APPlaces_ESG As Decimal
        Dim result = Decimal.Zero
        'ESG does Not exist In 1718 so this has been Set To zero
        'If currentscenario.periodid = 2017181 And (F100_AllAcademies = 17181 Or F100_AllAcademies = 17182 Or F100_AllAcademies = 17183) then    
        '   Result = NOR_P19_HND_AP_Pre16
        'Else Exclude(rid)
        'End If
        result = 0
        Return result
    End Function

    <Calculation(Id:="40662c1d337b406189466ef231b9c654")>
    Public Function NOR_P31_Total_NOR_HospitalPlaces_ESG As Decimal
        Dim result = Decimal.Zero
        'ESG does Not exist In 1718 so this has been Set To zero
        'If currentscenario.periodid = 2017181 And (F100_AllAcademies = 17181 Or F100_AllAcademies = 17182 Or F100_AllAcademies = 17183) then    
        '   Result = NOR_P20_HND_Hosp_Pl
        'Else Exclude(rid)
        'End If 
        result = 0
        Return result
    End Function

    <Calculation(Id:="39747377743a4816aa224fddd0b7e3f0")>
    Public Function NOR_P33_1617_Base_NOR As Decimal
        Dim result = Decimal.Zero
        Dim Scenario_Report_P26 As Decimal = Persisted.Report_AY1617_Acad_NOR : NOR_AY201617_Report.P26_Total_NOR_SBS
        Dim Scenario_Report_P01 As Decimal = Persisted.Report_AY1617_Acad_NOR : NOR_AY201617_Report.P01_NOR_RU
        If currentscenario.periodid = 2017181 And (F100_AllAcademies = 17181 Or F100_AllAcademies = 17182 Or F100_AllAcademies = 17183) then
            result = Scenario_Report_P26 - Scenario_Report_P01
        Else
            Exclude(rid)
        End If

        Return result
    End Function

    <Calculation(Id:="b7f8685a92b3470487ef6c919993f4be")>
    Public Function NOR_P34_1617_Base_RU As Decimal
        Dim result = Decimal.Zero
        Dim Scenario_Report_P01 As Decimal = Persisted.Report_AY1617_Acad_NOR : NOR_AY201617_Report.P01_NOR_RU
        If currentscenario.periodid = 2017181 And (F100_AllAcademies = 17181 Or F100_AllAcademies = 17182 Or F100_AllAcademies = 17183) then
            result = Scenario_Report_P01
        Else
            Exclude(rid)
        End If

        Return result
    End Function

    <Calculation(Id:="cbce4b4eed0144ab9b9ca1e0dad2f1bf")>
    Public Function NOR_P36_1617_pre16_HN As Decimal
        Dim result = Decimal.Zero
        Dim Scenario_Report_P18 As Decimal = Persisted.Report_AY1617_Acad_NOR : NOR_AY201617_Report.P18_NOR_HNP_HN_Pre16
        If currentscenario.periodid = 2017181 And (F100_AllAcademies = 17181 Or F100_AllAcademies = 17182 Or F100_AllAcademies = 17183) then
            result = Scenario_Report_P18
        Else
            Exclude(rid)
        End If

        Return result
    End Function

    <Calculation(Id:="7d340baa040343d0831042b3e9f5573a")>
    Public Function NOR_P37_1617_pre16_AP As Decimal
        Dim result = Decimal.Zero
        Dim Scenario_Report_P19 As Decimal = Persisted.Report_AY1617_Acad_NOR : NOR_AY201617_Report.P19_NOR_HNP_AP_Pre16
        If currentscenario.periodid = 2017181 And (F100_AllAcademies = 17181 Or F100_AllAcademies = 17182 Or F100_AllAcademies = 17183) then
            result = Scenario_Report_P19
        Else
            Exclude(rid)
        End If

        Return result
    End Function

    <Calculation(Id:="d65d89712df84ddc8fa23ee0e69799b1")>
    Public Function NOR_P38_1617_HN_Hosp_Pl As Decimal
        Dim result = Decimal.Zero
        Dim Scenario_Report_P20 As Decimal = Persisted.Report_AY1617_Acad_NOR : NOR_AY201617_Report.P20_NOR_HNP_Hosp_Pl
        If currentscenario.periodid = 2017181 And (F100_AllAcademies = 17181 Or F100_AllAcademies = 17182 Or F100_AllAcademies = 17183) then
            result = Scenario_Report_P20
        Else
            Exclude(rid)
        End If

        Return result
    End Function

    <Calculation(Id:="91d0e7b2c84b4199aed380b8ebb2e818")>
    Public Function NOR_P39_1617_MFG_NOR As Decimal
        Dim result = Decimal.Zero
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

    <Calculation(Id:="526e0a552b3e4af1ab559a6c0e04c08e")>
    Public Function NOR_P40_1617_Post16_NOR As Decimal
        Dim result = Decimal.Zero
        Dim Scenario_Report_P03 As Decimal = Persisted.Report_AY1617_Acad_Post16 : Post16_AY201617_Report.P03_Learners
        If currentscenario.periodid = 2017181 And (F100_AllAcademies = 17181 Or F100_AllAcademies = 17182 Or F100_AllAcademies = 17183) then
            result = Scenario_Report_P03
        Else
            Exclude(rid)
        End If

        Return result
    End Function

    <Calculation(Id:="95a34730babb488d92b694d62f892125")>
    Public Function NOR_P41_1617_Post16_HN As Decimal
        Dim result = Decimal.Zero
        Dim Scenario_Report_P04 As Decimal = Persisted.Report_AY1617_Acad_Post16 : Post16_AY201617_Report.P04_HNPlaces
        If currentscenario.periodid = 2017181 And (F100_AllAcademies = 17181 Or F100_AllAcademies = 17182 Or F100_AllAcademies = 17183) then
            result = Scenario_Report_P04
        Else
            Exclude(rid)
        End If

        Return result
    End Function

    <Calculation(Id:="fd752d78c0fa4be5b40cf66e33b9ad96")>
    Public Function NOR_P51_Total_NOR_Mainstream As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim NOR_P02 As Decimal = NOR_P02_PRI
        Dim NOR_P05 As Decimal = NOR_P05_NUR
        Dim NOR_P06 As Decimal = NOR_P06_SEC
        Dim NOR_P21 As Decimal = NOR_P21_P16
        If currentscenario.periodid = 2017181 And F100_AllAcademies = 17181
            result = NOR_P02 + NOR_P05 + NOR_P06 + NOR_P21
        ElseIf currentscenario.periodid = 2017181 And (F100_AllAcademies = 17182 Or F100_AllAcademies = 17183) then
            result = NOR_P02 + NOR_P05 + NOR_P06 + NOR_P21
        Else
            Exclude(rid)
        End If

        Return result
    End Function

    <Calculation(Id:="342b8021a49042f2bcb28f856128692e")>
    Public Function NOR_P52_Total_NOR_HN_Places As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim NOR_P18 As Decimal = NOR_P18_HND_HN_Pre16
        If currentscenario.periodid = 2017181 And (F100_AllAcademies = 17181 Or F100_AllAcademies = 17182 Or F100_AllAcademies = 17183) then
            result = NOR_P18
        Else
            Exclude(rid)
        End If

        Return result
    End Function

    <Calculation(Id:="53c87ae6c5be4c408540f4e5b13f2c1b")>
    Public Function NOR_P53_Total_NOR_AP_Places As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim NOR_P19 As Decimal = NOR_P19_HND_AP_Pre16
        If currentscenario.periodid = 2017181 And (F100_AllAcademies = 17181 Or F100_AllAcademies = 17182 Or F100_AllAcademies = 17183) then
            result = NOR_P19
        Else
            Exclude(rid)
        End If

        Return result
    End Function

    <Calculation(Id:="8e303c98b2d54cabbef47823b8d4d226")>
    Public Function NOR_P54_Total_NOR_Hospital_Places As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim NOR_P20 As Decimal = NOR_P20_HND_Hosp_Pl
        If currentscenario.periodid = 2017181 And (F100_AllAcademies = 17181 Or F100_AllAcademies = 17182 Or F100_AllAcademies = 17183) then
            result = NOR_P20
        Else
            Exclude(rid)
        End If

        Return result
    End Function

    <Calculation(Id:="a2841376bdfb4bd88934745d7e2260e3")>
    Public Function NOR_P32_Total_NOR_ESGProt As Decimal
        Dim result = Decimal.Zero
        Dim P22_Total_NOR_Pri_SBS As Decimal = NOR_P22_Total_NOR_PRI_SBS
        Dim P25_Total_NOR_Sec_SBS As Decimal = NOR_P25_Total_NOR_SEC_SBS
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

    <Calculation(Id:="cf97958488f84c729247823a2ac1dbc1")>
    Public Function NOR_P32b_TOTAL_ESGProt_incHN As Decimal
        Dim result = Decimal.Zero
        Dim NOR_P02_Pri As Decimal = NOR_P02_PRI
        Dim NOR_P06_Sec As Decimal = NOR_P06_SEC
        Dim Tot_Places As Decimal = NOR_P18_HND_HN_Pre16 + NOR_P20_HND_Hosp_Pl + NOR_P19_HND_AP_Pre16
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

    <Calculation(Id:="f58e84ec82ae48f6a6164c24d529257e")>
    Public Function NOR_P35_1617_BaseNOR_ESGProt As Decimal
        Dim result = Decimal.Zero
        Dim Date_Opened As Date = Provider.DateOpened
        Dim NOR_1617_Scenario_Report_P32_Total_NOR_ALP As Decimal = Persisted.Report_AY1617_Acad_NOR : NOR_AY201617_Report.P32_Total_NOR_ALP
        Print(Date_Opened, "Date Opened", rid)
        If currentscenario.periodid = 2017181 And (F100_AllAcademies = 17181 Or F100_AllAcademies = 17182 Or F100_AllAcademies = 17183) then
            result = NOR_1617_Scenario_Report_P32_Total_NOR_ALP
        Else
            Exclude(rid)
        End If

        Return result
    End Function

    <Calculation(Id:="c347491556354828964a0c2490a4f351")>
    Public Function NOR_P35b_1617_BaseNOR_ESGProtincHN As Decimal
        Dim result = Decimal.Zero
        Dim Date_Opened As Date = Provider.DateOpened
        Dim NOR_1617_Scenario_Report_P32b_Total_NOR_ALPincHN As Decimal = Persisted.Report_AY1617_Acad_NOR : NOR_AY201617_Report.P32b_Total_NOR_ALP_incHN
        Print(Date_Opened, "Date Opened", rid)
        If currentscenario.periodid = 2017181 And (F100_AllAcademies = 17181 Or F100_AllAcademies = 17182 Or F100_AllAcademies = 17183) then
            result = NOR_1617_Scenario_Report_P32b_Total_NOR_ALPincHN
        Else
            Exclude(rid)
        End If

        Return result
    End Function

    <Calculation(Id:="c7e3592320af427e87d97314f5ddbbd7")>
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

    <Calculation(Id:="350ba3bcf15f4450bf81de04bdb239d3")>
    Public Function NOR_P46_SEC As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim NOR_P25 As Decimal = NOR_P25_Total_NOR_SEC_SBS
        If currentscenario.periodid = 2017181 And (F100_AllAcademies = 17181 Or F100_AllAcademies = 17182 Or F100_AllAcademies = 17183) then
            result = NOR_P25
        Else
            Exclude(rid)
        End If

        Return result
    End Function

    <Calculation(Id:="158f3550b13745629df471bf61cf78f0")>
    Public Function NOR_P47_KS3 As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim NOR_P23 As Decimal = NOR_P23_Total_NOR_KS3_SBS
        If currentscenario.periodid = 2017181 And (F100_AllAcademies = 17181 Or F100_AllAcademies = 17182 Or F100_AllAcademies = 17183) then
            result = NOR_P23
        Else
            Exclude(rid)
        End If

        Return result
    End Function

    <Calculation(Id:="29c88072a49d478882c1a6b32d21bc9b")>
    Public Function NOR_P48_KS4 As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim NOR_P24 As Decimal = NOR_P24_Total_NOR_KS4_SBS
        If currentscenario.periodid = 2017181 And (F100_AllAcademies = 17181 Or F100_AllAcademies = 17182 Or F100_AllAcademies = 17183) then
            result = NOR_P24
        Else
            Exclude(rid)
        End If

        Return result
    End Function

    <Calculation(Id:="caedefe8ac574ab69e529ba65b498a69")>
    Public Function NOR_P49_Y1toY4 As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim NOR_P03 As Decimal = NOR_P03_Y1Y4
        If currentscenario.periodid = 2017181 And (F100_AllAcademies = 17181 Or F100_AllAcademies = 17182 Or F100_AllAcademies = 17183) then
            result = NOR_P03
        Else
            Exclude(rid)
        End If

        Return result
    End Function

    <Calculation(Id:="9d7accfacff5419ca3f30ddfd31b65a8")>
    Public Function NOR_P50_Y5toY6 As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim NOR_P04 As Decimal = NOR_P04_Y5Y6
        If currentscenario.periodid = 2017181 And (F100_AllAcademies = 17181 Or F100_AllAcademies = 17182 Or F100_AllAcademies = 17183) then
            result = NOR_P04
        Else
            Exclude(rid)
        End If

        Return result
    End Function
End Class
