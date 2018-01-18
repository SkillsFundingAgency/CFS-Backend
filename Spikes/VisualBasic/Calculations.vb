Imports System

Public Class Calculations
    Inherits BaseCalculation

    Public Property Datasets As Datasets

    <Calculation(Id:="a3e58080dc9d4e779ed7619e03896afb")>
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

    <Calculation(Id:="3ac905726a1c4ca288405c03a64bd196")>
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

    <Calculation(Id:="49929977203245ff9ca0948a37ff66bc")>
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

    <Calculation(Id:="29a5cc8bc0824d40a3a1649fbf1f6e38")>
    <CalculationSpecification(Id:="P006a_NSEN_PriBE_Percent", Name:="P006a_NSEN_PriBE_Percent")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="bb411f3c107a40a8a71ff57604296e0e", Name:="Basic Entitlement")>
    Public Function P006a_NSEN_PriBE_Percent As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="05984f32eada42a5a653af313b7d9046")>
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

    <Calculation(Id:="74b9c1b7295b4a2d864f8f6983dd9591")>
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

    <Calculation(Id:="d2b4a40e2ffb426fbe6a9d8cbf2ebd41")>
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

    <Calculation(Id:="2b86cfa41567479c81afa5df440ce673")>
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

    <Calculation(Id:="8662f2536f634b4c80be5e90dbbb362c")>
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

    <Calculation(Id:="69f6fee06e754f339018fe8bdec52237")>
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

    <Calculation(Id:="a2742b6bf3fe47948380a660de296c6c")>
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

    <Calculation(Id:="763db90b17634b4290dff9288ea1c655")>
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

    <Calculation(Id:="d05a815506444d44bb341a7aa67b0288")>
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

    <Calculation(Id:="5278956d315447e18264f93c8b8e9f7c")>
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

    <Calculation(Id:="4f6c57d862ff4453aefe89825518dc79")>
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

    <Calculation(Id:="0acd002fab6c4a57a6ea0521be166713")>
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

    <Calculation(Id:="8b66fda63cd1482cacda8407fcab3d40")>
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

    <Calculation(Id:="6bfa86639cc141cabdcffbb098eaf597")>
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

    <Calculation(Id:="96cd03f3717d4381be351fb542d1c4a7")>
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

    <Calculation(Id:="d970e615a0844815894a6e615f4690ed")>
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

    <Calculation(Id:="f44021515d924341915bd3dca2ee9446")>
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

    <Calculation(Id:="0214143c327444d6bf0f874a81b4682f")>
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

    <Calculation(Id:="0e103e29343f411c9113e52a87bc9439")>
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

    <Calculation(Id:="9888953dff3242bfae74fb0a2b11b154")>
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

    <Calculation(Id:="a5169e60f0d84afe8fc8e1cc59c2bab8")>
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

    <Calculation(Id:="7feb3686b3e44f80891d6dd2e33a7909")>
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

    <Calculation(Id:="66b82e99c5a6470894a0a136afffa422")>
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

    <Calculation(Id:="a10688b5897a4c22967f2ef9638ae445")>
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

    <Calculation(Id:="aaeaf50a6ce84c1ab6e215ae776f5292")>
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

    <Calculation(Id:="2f19093c45614f8eba77bd836ab64c95")>
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

    <Calculation(Id:="fa44c40ec29a4626b21f2f7690b0ed72")>
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

    <Calculation(Id:="69e34aff86d64c999731d9d35e213ade")>
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

    <Calculation(Id:="0d42df3c49794eb89075dae51f4cbebd")>
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

    <Calculation(Id:="f354d90eb1964025898fde44d54975c8")>
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

    <Calculation(Id:="69afbd9701964c9d8c0638a0adb9802a")>
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

    <Calculation(Id:="4a0b8f936b8f447599e67f9ef9e665c3")>
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

    <Calculation(Id:="c53fda93d25e402e966d9101138cd128")>
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

    <Calculation(Id:="de1f773367b14b62a7a7621a0f6de58a")>
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

    <Calculation(Id:="2facaf0c3dc7465fb8abd9b9764cd248")>
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

    <Calculation(Id:="83b130c206264b9caa0909e5ac1b37e4")>
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

    <Calculation(Id:="9592d010fdf24eef974484f178163526")>
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

    <Calculation(Id:="8ebab57058644d0ebf46a3f449981716")>
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

    <Calculation(Id:="a47148b82c3a4d21bba9361dbbdea68e")>
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

    <Calculation(Id:="3f4c59ca1b114aa6a45545fce615c3b3")>
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

    <Calculation(Id:="d09defc565d547cebba188aafe3dd500")>
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

    <Calculation(Id:="5ff619a4681742f688764c053e61d741")>
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

    <Calculation(Id:="bf5a0505f1f849248c3d8085fb0ae79c")>
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

    <Calculation(Id:="08c687aabce748b3b4fb420d862ec502")>
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

    <Calculation(Id:="367ed07333ea402b98bb43257d1d3464")>
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

    <Calculation(Id:="ad7b3a321653413aac983aed8e990389")>
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

    <Calculation(Id:="f3743ff67d7841cab837266f584a2fe8")>
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

    <Calculation(Id:="850849c8b1c04251a46b11d583c95fdb")>
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

    <Calculation(Id:="6be7219fc77b47e1a1ab5d2c143d2c39")>
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

    <Calculation(Id:="e669b54fbc354abeb3fd9403b9e2f3a4")>
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

    <Calculation(Id:="1e7c549f9639424fa8640efc846618db")>
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

    <Calculation(Id:="36dbd5bae8dc49f18ec6388ba736da94")>
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

    <Calculation(Id:="679b0a58c8b94e288d814517db8d4276")>
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

    <Calculation(Id:="7b46d12f8c0b40d3b89033c102ca9788")>
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

    <Calculation(Id:="7ddb0f35dfc4425abcea269fafc77594")>
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

    <Calculation(Id:="f35d90f0abd04d6a8f7f5a1d9bfc3427")>
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

    <Calculation(Id:="9393bf594168444ca7a050b00ca3a9a5")>
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

    <Calculation(Id:="b24f5401d2694870a1bc36c99998f77b")>
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

    <Calculation(Id:="94223102757f435089abbbb0d50d2cf7")>
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

    <Calculation(Id:="59e2ac2d6524479ca6cb9d07015bbb45")>
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

    <Calculation(Id:="4d1c6a7a3904490eb4fc122eae5beba7")>
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

    <Calculation(Id:="b8bced4f90744e3ab9a05526500e7398")>
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

    <Calculation(Id:="1188e27ff9a0415599e244e960452515")>
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

    <Calculation(Id:="b8e737b307ea4148b1fa415a86b4b786")>
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

    <Calculation(Id:="3c673748e0c848c1ba5b05d0af7486ef")>
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

    <Calculation(Id:="c310d803986c49238aa3c8b3cccd6b71")>
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

    <Calculation(Id:="cb24795de24a4d1c9d2be809aeec93d5")>
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

    <Calculation(Id:="69b58269592d454ebc54522a69e3178a")>
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

    <Calculation(Id:="9a97109e3d624f88ac5f23a540e54112")>
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

    <Calculation(Id:="ee09945573034580b7bfc07f8f54ad8e")>
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

    <Calculation(Id:="8e4df13e15e244bb9d590431b36d39b6")>
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

    <Calculation(Id:="efa6d49884d344abbdb7769ff774d740")>
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

    <Calculation(Id:="3f9a7acfc7f742fdbac1ed4f40c52502")>
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

    <Calculation(Id:="d24cdba20ff54364bd797b82b1d3e55f")>
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

    <Calculation(Id:="2b0c5c2ae20345669368eaca4d552ce2")>
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

    <Calculation(Id:="dcc630dbc6c54779b24c48c0ae36294a")>
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

    <Calculation(Id:="7657fe7289cc4fc6974720a56f004253")>
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

    <Calculation(Id:="99ddc6aad4a9475b8cadab412dc77136")>
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

    <Calculation(Id:="fc5a1f47049f4d86b3c88cd1c4c66b8f")>
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

    <Calculation(Id:="afe161b0743c4db0b82a458dfba664e5")>
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

    <Calculation(Id:="dc561546362f4a7ea2fd5ffa3231e1a8")>
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

    <Calculation(Id:="a276c05cc60d4c1abb7844181fc4221d")>
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

    <Calculation(Id:="90ae071f15dc478490137434e9a95e38")>
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

    <Calculation(Id:="bf689e10a5264c1daa4af71b420c8c8c")>
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

    <Calculation(Id:="4ed1cd8eaf8944f1a5caa66763149ff4")>
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

    <Calculation(Id:="50470e7697e04babb795a741e28297ed")>
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

    <Calculation(Id:="106c2fe781454e61b880724661dbe453")>
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

    <Calculation(Id:="7224befd0992461ebac14cf779d00540")>
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

    <Calculation(Id:="cba9a583711748f189397dc4dc404ab7")>
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

    <Calculation(Id:="7d9b61e54ffe4b109fd9565820bba64c")>
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

    <Calculation(Id:="4c79c7b8e8894926a4a7f12cbec767af")>
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

    <Calculation(Id:="b8f66c66bdd84d76b1f1e0ae178305f1")>
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

    <Calculation(Id:="6c2234476c7845d3868dffb16b1b167e")>
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

    <Calculation(Id:="93e87001a52e4e4fa73042cc9b4bb1e4")>
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

    <Calculation(Id:="1fd2b405c5a6405583afbc503a05964a")>
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

    <Calculation(Id:="67de66d22c2c442686e2a78fc2316c02")>
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

    <Calculation(Id:="3275c99c85974ba4b9782eabfaa1089b")>
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

    <Calculation(Id:="806eb687cbc44f46b638f142065e75d8")>
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

    <Calculation(Id:="87480856cbd547f9b82e7ae4ba4af935")>
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

    <Calculation(Id:="55bdf53d0c6d4fdb9df3a80c8daf6fe5")>
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

    <Calculation(Id:="1d490ee900e44b68a40a2c3a7c40fb01")>
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

    <Calculation(Id:="6db07808d3304a06952e3a9515644068")>
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

    <Calculation(Id:="ca4608c7f91249caa2c3845b709cda36")>
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

    <Calculation(Id:="b1abdfa4112140418e0ea194c282a758")>
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

    <Calculation(Id:="dddcc0a433ed478c8cacfe18fc8b2262")>
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

    <Calculation(Id:="5206e501e3c74147a848e2b7f29c5dc8")>
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

    <Calculation(Id:="2c5b31925d3c4c78bc45f78d803ba603")>
    <CalculationSpecification(Id:="P87a_NSENIDACIESec_Percent", Name:="P87a_NSENIDACIESec_Percent")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="86e2181721da4a669a09cccbafc502f1", Name:="IDACI")>
    Public Function P87a_NSENIDACIESec_Percent As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="655f58bdb8c04106b6b6ff54e6691a9f")>
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

    <Calculation(Id:="c2ff295a3efd4619a084a137647ee390")>
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

    <Calculation(Id:="c2ebcfd9dee74815b3e6825b47f73092")>
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

    <Calculation(Id:="ba7b491d83d049969de80726f1fa9458")>
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

    <Calculation(Id:="0b68d6bcdadc40b8aa90101f54d67fcf")>
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

    <Calculation(Id:="280a91060ff340b2a01ebcd50ae358b9")>
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

    <Calculation(Id:="d7afd05ae1db40a8b735404dc691d64c")>
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

    <Calculation(Id:="6b9f19fea1c74de192846782ccc9379a")>
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

    <Calculation(Id:="053d6b4aaf9e4752b36fd34f0ecae41d")>
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

    <Calculation(Id:="069b96920a72415d9e7a42691683d6af")>
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

    <Calculation(Id:="8ba63433f9f24fafacb4f873611b33ae")>
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

    <Calculation(Id:="7c1b0b87a06b4699a821382076e6a6f3")>
    <CalculationSpecification(Id:="P099a_NSENIDACICSec_Percent", Name:="P099a_NSENIDACICSec_Percent")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="86e2181721da4a669a09cccbafc502f1", Name:="IDACI")>
    Public Function P099a_NSENIDACICSec_Percent As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="abafaaaf6e194dfe8f9c1807fccb85c0")>
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

    <Calculation(Id:="91085b108d574694b4ad411033b82596")>
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

    <Calculation(Id:="a9780a96effc42a7aeed7e7d2adb8fe5")>
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

    <Calculation(Id:="d54ec8f7096e4b89bd5cf0d368227778")>
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

    <Calculation(Id:="6a1df7d3b2414a0a9cc6bc5aa8a911c7")>
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

    <Calculation(Id:="75611766b94f4efeb5fdaa368df6fc60")>
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

    <Calculation(Id:="434057c2c79d46f188e38d143e42a084")>
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

    <Calculation(Id:="9fcc2667aeb047e7b82c0f31adbc6e6b")>
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

    <Calculation(Id:="4e68f8646ebb4a77a756e113e97c9bf8")>
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

    <Calculation(Id:="381c5853dd39473d8ace3e654fe39253")>
    <CalculationSpecification(Id:="P110_IDACIASecSubtotal", Name:="P110_IDACIASecSubtotal")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="86e2181721da4a669a09cccbafc502f1", Name:="IDACI")>
    Public Function P110_IDACIASecSubtotal As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="104f7b23a619467c83a11d485bc645e3")>
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

    <Calculation(Id:="4839c2e5e8954587a94f13f941aa9778")>
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

    <Calculation(Id:="937da400614448529b48f920ae11be82")>
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

    <Calculation(Id:="50e0f45a4f114302a8b11b21363f62e7")>
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

    <Calculation(Id:="0c3da83db5f64551873d406f1e0631e3")>
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

    <Calculation(Id:="9baa8a0c940e44419e7b64547354e427")>
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

    <Calculation(Id:="30fb1a2c5d0641a3b16984c8c1a28dd0")>
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

    <Calculation(Id:="3f3c4cf8f3e7476b9724bf091d7ced78")>
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

    <Calculation(Id:="72f8a11d3a5343109ce3f73a8c8d80a4")>
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

    <Calculation(Id:="1f29c3f578fb42908e1351f15c59c19d")>
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

    <Calculation(Id:="52d220069f4b43708c7fb3fdd7b46eca")>
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

    <Calculation(Id:="b72c668b5fef4746b0f9fe209366082f")>
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

    <Calculation(Id:="80d02b02eb4a43b8be7b875eefc223e5")>
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

    <Calculation(Id:="39ceacd42a3f474fbecfcde2463fed44")>
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

    <Calculation(Id:="6b0c0b6988b54d8fb32e0d44edafccc3")>
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

    <Calculation(Id:="93a3ed61bf03449ba5d57bbbd3ad09d5")>
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

    <Calculation(Id:="1ba02247ebca4d849fe25cf7eb70a7a7")>
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

    <Calculation(Id:="d590ed516ca5466a9c9840aa37d8718e")>
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

    <Calculation(Id:="1eb5a4f904af41be9b9cd504f3bddc3f")>
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

    <Calculation(Id:="8a35516ba46844c284fcdb734b4e832f")>
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

    <Calculation(Id:="0f5287e161534f0593da0684c815d899")>
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

    <Calculation(Id:="9c1f7fac297d483ebb4b3894159162a0")>
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

    <Calculation(Id:="361c00f45c1d4c44a0d00871871cb9d4")>
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

    <Calculation(Id:="fcf5eb1ecb4c40ae901f6d9ebefff6bc")>
    <CalculationSpecification(Id:="P241_Primary_Lump_Sum", Name:="P241_Primary_Lump_Sum")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="41080d8da69046e3a26ff4fb6894eaa8", Name:="Other Factors")>
    Public Function P241_Primary_Lump_Sum As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="e5835a8a426a43cca8015a40297200ec")>
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

    <Calculation(Id:="e5b07758e6a740feb770e0783a1799ed")>
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

    <Calculation(Id:="ff270d56f197411da95946a1ea2ea970")>
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

    <Calculation(Id:="ab206a571def4e59a8a447ec4a29a0b6")>
    <CalculationSpecification(Id:="P245_Secondary_Lump_Sum", Name:="P245_Secondary_Lump_Sum")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="41080d8da69046e3a26ff4fb6894eaa8", Name:="Other Factors")>
    Public Function P245_Secondary_Lump_Sum As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="2774c9e24c4a4b0ea8b2a9ec98b2f6f5")>
    <CalculationSpecification(Id:="P246_In YearSecLumpSumSubtotal", Name:="P246_In YearSecLumpSumSubtotal")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="41080d8da69046e3a26ff4fb6894eaa8", Name:="Other Factors")>
    Public Function P246_InYearSecLumpSumSubtotal As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="fd21ef08c7254310a3c92c515a5216d1")>
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

    <Calculation(Id:="99bced99a160461b9eddad99e8b2f607")>
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

    <Calculation(Id:="41bfdfe1e0364b72bfa5389d41cca975")>
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

    <Calculation(Id:="b8eeb09d3695460aad6143286443b754")>
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

    <Calculation(Id:="cc84de48ef4b4b47965676369b2083b1")>
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

    <Calculation(Id:="3111d384de2d4f12b58cc5a020b41f60")>
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

    <Calculation(Id:="571d6fd78c7645b0b3e1d891ff670d0e")>
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

    <Calculation(Id:="3a258812b7834552931721f430af6d2c")>
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

    <Calculation(Id:="9a1b0bb200c1487695f16ac9e1021c87")>
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

    <Calculation(Id:="95b17409786342438f47ec73d52483cd")>
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

    <Calculation(Id:="2a86e72289c24dff9f06cced4abd8519")>
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

    <Calculation(Id:="9811e66a8d7749af8857e47e9bcd8d2b")>
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

    <Calculation(Id:="f8cf454396394366a519c19b7a6d307a")>
    <CalculationSpecification(Id:="P262a_NSENEx1_Percent", Name:="P262a_NSENEx1_Percent")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="41080d8da69046e3a26ff4fb6894eaa8", Name:="Other Factors")>
    Public Function P262a_NSENEx1_Percent As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="6646dd3063bf402b9bc02be274b8aec9")>
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

    <Calculation(Id:="25c8e644873248d48d6ae46077e3827c")>
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

    <Calculation(Id:="90d8fc0a73e341999967d73288400054")>
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

    <Calculation(Id:="799a2b23a8b04871a2642aad47f84d79")>
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

    <Calculation(Id:="e3de5a9570554933a2c61873e603f14e")>
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

    <Calculation(Id:="ae0da4c1345e4393ad25b5db3f90ef98")>
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

    <Calculation(Id:="c6c23a4368934ecbbb9caf10a5c1d54b")>
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

    <Calculation(Id:="59a51e31f88240ada10635f5940ed4b3")>
    <CalculationSpecification(Id:="P270a_NSENEx3_Percent", Name:="P270a_NSENEx3_Percent")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="41080d8da69046e3a26ff4fb6894eaa8", Name:="Other Factors")>
    Public Function P270a_NSENEx3_Percent As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="38e77a5e449047d3ac7f42feb8f65e50")>
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

    <Calculation(Id:="b7f54a3d538d40c29ee6861eb38e7364")>
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

    <Calculation(Id:="7a2d015c88f24bdbb5f0a84e17e713ad")>
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

    <Calculation(Id:="256d52b69abe4156a935ef88169a712f")>
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

    <Calculation(Id:="eea1b21d67194540a2c55ac0b40201a2")>
    <CalculationSpecification(Id:="P275_InYearEx4Subtotal", Name:="P275_InYearEx4Subtotal")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="41080d8da69046e3a26ff4fb6894eaa8", Name:="Other Factors")>
    Public Function P275_InYearEx4Subtotal As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="9b7f1396697147bc84d7511ed91a3870")>
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

    <Calculation(Id:="a00ae4dab4d64d8c91c753edf2e1cf7f")>
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

    <Calculation(Id:="ac421f481c26440da41c6450091cebf8")>
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

    <Calculation(Id:="f58364c67611471fb0e3236052d9e451")>
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

    <Calculation(Id:="0faa4d0684f744beb330a121fbc0ba8d")>
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

    <Calculation(Id:="1ed559f833ab4affb7b08c34ac34f839")>
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

    <Calculation(Id:="9df4ce6b2c9840eea1c0f0e4dd19e73a")>
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

    <Calculation(Id:="1ed20e42feb143fb86a33d69153afe83")>
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

    <Calculation(Id:="7a2579c2713740afbc0414329e6263e6")>
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

    <Calculation(Id:="055eede3d07f4476ae93997282847301")>
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

    <Calculation(Id:="ef47879233a2467f870782686fd4f653")>
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

    <Calculation(Id:="ff47bd7631ba44bcbc86dba051a1628e")>
    <CalculationSpecification(Id:="P287_InYearPriorYearAdjsutmentSubtotal", Name:="P287_InYearPriorYearAdjsutmentSubtotal")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="41080d8da69046e3a26ff4fb6894eaa8", Name:="Other Factors")>
    Public Function P287_InYearPriorYearAdjsutmentSubtotal As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="65dd36f0c40f41c09da24e2b9ab09bab")>
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

    <Calculation(Id:="ff951a462f5f4e05abc3c148479b089b")>
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

    <Calculation(Id:="d618ce370c52400383ae10739a48053c")>
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

    <Calculation(Id:="8b9845a9af304512ae0355a44a958353")>
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

    <Calculation(Id:="e126d6ccd49f4fe293ed98da6db1511d")>
    <CalculationSpecification(Id:="P120_PPAindicator", Name:="P120_PPAindicator")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="c6b57f57424c494fb0ed394ffb1af34c", Name:="Prior Attainment")>
    Public Function P120_PPAindicator As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="d179cfcdb36a43fc98e71259018cc66b")>
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

    <Calculation(Id:="108192723a2d46b68d607119dbbf8424")>
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

    <Calculation(Id:="805a5afe5ba94cac8493eeda293d9744")>
    <CalculationSpecification(Id:="P122a_PPAY7378forFAPOnly", Name:="P122a_PPAY7378forFAPOnly")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="c6b57f57424c494fb0ed394ffb1af34c", Name:="Prior Attainment")>
    Public Function P122a_PPAY7378forFAPOnly As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="afad63159f5641c282d6c377e4efcbfe")>
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

    <Calculation(Id:="f06f2330c9f04915a38867e48cde42b1")>
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

    <Calculation(Id:="13af97f7a7b34318b7fc2a6b9f6ecc74")>
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

    <Calculation(Id:="04ef6789b2e34b24a464f9cd2f81b6b9")>
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

    <Calculation(Id:="46f8d46742d44dab8731fdbe85abf13c")>
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

    <Calculation(Id:="8984ab6196d84c668edd0c7847fbe22e")>
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

    <Calculation(Id:="5b9ce0cb023b46d5befe6a5aff114464")>
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

    <Calculation(Id:="aec83ead16ea47f797b0670d946b0d16")>
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

    <Calculation(Id:="da094df3b5504bcd9ac69fd4c29453ea")>
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

    <Calculation(Id:="0ef731909d3146f8a7f7159dbb21a1e0")>
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

    <Calculation(Id:="03d3f701449348ba80866da68a7d9a94")>
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

    <Calculation(Id:="36a5ce32f9e940b6b8539a4b51b4804f")>
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

    <Calculation(Id:="c4ad5db021c94cc5b393077fe70fd06e")>
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

    <Calculation(Id:="178b57122e134a2ab8a8293038fa1651")>
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

    <Calculation(Id:="2bf14bcf23714e66b8ac14411c49c252")>
    <CalculationSpecification(Id:="P136_SecPA_Y7Factor", Name:="P136_SecPA_Y7Factor")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="c6b57f57424c494fb0ed394ffb1af34c", Name:="Prior Attainment")>
    Public Function P136_SecPA_Y7Factor As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="2765848c3d1a40c78c701a3eb3255f34")>
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

    <Calculation(Id:="bcb2a3776bd34ef9b0d69922f2aab4aa")>
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

    <Calculation(Id:="7472880ed82248afb1c48eb007659f0a")>
    <CalculationSpecification(Id:="P138a_SecPA_AdjustedSecFactor", Name:="P138a_SecPA_AdjustedSecFactor")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="c6b57f57424c494fb0ed394ffb1af34c", Name:="Prior Attainment")>
    Public Function P138a_SecPA_AdjustedSecFactor As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim F200 As Decimal = F200_SBS_Academies
        Dim P08a_Y7 As Decimal = NOR_P08a_Y7
        Dim P08b_Y8to11 As Decimal = NOR_P08b_Y8to11
        Dim P136_SecPA_Y7Factor As Decimal = P136_SecPAFactor_Y7Factor
        If F200 = 1 then
            result =((P136_SecPA_Y7Factor * P08a_Y7 * P136a_SecPA_Y7NationalWeight) + (P137_SecPA_Y8to11Factor * P08b_Y8to11)) / (P08a_Y7 + P08b_Y8to11)
        Else
            exclude(rid)
        End if

        Return result
    End Function

    <Calculation(Id:="ce319f3cb14241fd8e9ec1dcf5b433f6")>
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

    <Calculation(Id:="73b3efeb49944e599fa97754efa91ea0")>
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

    <Calculation(Id:="3b527a7968914a4791eae2ebf61c7576")>
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

    <Calculation(Id:="11596e1f2c84406b919296f18c03fe65")>
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

    <Calculation(Id:="c9a767aacd4d4d0e8fc2612bb778f61d")>
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

    <Calculation(Id:="3009b66693b64fe29ce664001756c7a2")>
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

    <Calculation(Id:="c2f8e3a008204545b0dae7c5d27eef37")>
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

    <Calculation(Id:="4546c7238d6e4301b9e71210a3ba930c")>
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

    <Calculation(Id:="0733ad57b32f4d8dadfb123b070ae84c")>
    <CalculationSpecification(Id:="P189_SparsityTaperFlagAllThru", Name:="P189_SparsityTaperFlagAllThru")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="4eda722dff5b4f6f9e3d13eae928be8a", Name:="Sparsity")>
    Public Function P189_SparsityTaperFlagAllThru As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="1a082901fee14f1fa31426c75ff8fe00")>
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

    <Calculation(Id:="9aa1e94551a84c258f4bf8f82f4444aa")>
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

    <Calculation(Id:="aca8bf4082d64d8986a5a5ee594a4090")>
    <CalculationSpecification(Id:="P192_SparsityDistThreshold", Name:="P192_SparsityDistThreshold")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="4eda722dff5b4f6f9e3d13eae928be8a", Name:="Sparsity")>
    Public Function P192_SparsityDistThreshold As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="dd31d9bc9bbd47949ec2c7b37ba23112")>
    <CalculationSpecification(Id:="P193_SparsityDistMet_YN", Name:="P193_SparsityDistMet_YN")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="4eda722dff5b4f6f9e3d13eae928be8a", Name:="Sparsity")>
    Public Function P193_SparsityDistMet_YN As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="89f290c40447450c8731854975f8c74b")>
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

    <Calculation(Id:="3f0374bb2a9d4a27a8d2195f4311cfcd")>
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

    <Calculation(Id:="db7813f934ca4d55b95a1cb7d5e388ed")>
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

    <Calculation(Id:="558e655e700f44e4a07857d5454bb1b0")>
    <CalculationSpecification(Id:="P197_SparsityLumpSumSubtotal", Name:="P197_SparsityLumpSumSubtotal")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="4eda722dff5b4f6f9e3d13eae928be8a", Name:="Sparsity")>
    Public Function P197_SparsityLumpSumSubtotal As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="0bd1322e59d6454ab813980d642ebbc5")>
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

    <Calculation(Id:="75041dff9f26437d982c45573f6c2d16")>
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

    <Calculation(Id:="ce1be7b65be845e4b898987745a3c8b8")>
    <CalculationSpecification(Id:="P199_InYearSparsityLumpSumSubtotal", Name:="P199_InYearSparsityLumpSumSubtotal")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="4eda722dff5b4f6f9e3d13eae928be8a", Name:="Sparsity")>
    Public Function P199_InYearSparsityLumpSumSubtotal As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="a23980b72dd24f6ebc27acb99566aac5")>
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

    <Calculation(Id:="8b1a0d99d9444ca09ee43bb3feda5d86")>
    <CalculationSpecification(Id:="P200a_InYear_SubtotalLump_Taper_for_FAP_Only", Name:="P200a_InYear_SubtotalLump_Taper_for_FAP_Only")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="4eda722dff5b4f6f9e3d13eae928be8a", Name:="Sparsity")>
    Public Function P200a_InYear_SubtotalLump_Taper_for_FAP_Only As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="7c6845f42b6546fa8a13a370efcbaf52")>
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

    <Calculation(Id:="01f7bc97e96148a09877d6b1e844e112")>
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

    <Calculation(Id:="45c405aee9d448bea491b45f6aab6457")>
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

    <Calculation(Id:="d30682e8c72545dab19a3cadde893548")>
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

    <Calculation(Id:="8ee1bb30d0f64c6c865e16e89ac2fbe0")>
    <CalculationSpecification(Id:="P249_SplitSiteSubtotal", Name:="P249_SplitSiteSubtotal")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="6857aab8747c4d699b69bba5858b98a3", Name:="Split Sites")>
    Public Function P249_SplitSiteSubtotal As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="6b4f51dcc7a84dcd8bb71f759c46edf1")>
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

    <Calculation(Id:="abb606ceee494af8b107859206b25aa0")>
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

    <Calculation(Id:="7af40d46928349b3b660e0435b436a8b")>
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

    <Calculation(Id:="dbd0e5263f5f4468a265aa88056065c4")>
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

    <Calculation(Id:="f8d18ec1b8fd4e67b43b819478d5fa68")>
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

    <Calculation(Id:="39ef0cb67df440f4b93749db4f47361a")>
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

    <Calculation(Id:="1177eccd7f404a31bf9d4691e622c49e")>
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

    <Calculation(Id:="12d56e47fd6c412dbdc7fc629cc96066")>
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

    <Calculation(Id:="5b3ff8de27d54bc1b5125ca7303213b9")>
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

    <Calculation(Id:="9711001ba89a47e7bea9f63240e0bfc1")>
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

    <Calculation(Id:="ae65195cbbe94414b8c147e7f2ac6250")>
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

    <Calculation(Id:="2f152e6c6bb14f85afc8cd1c9bd41b02")>
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

    <Calculation(Id:="74aebe99a4fd43ff82ca46e1590b2263")>
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

    <Calculation(Id:="fd3af79e48e048129027981a6e9549d4")>
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

    <Calculation(Id:="db23a75e0b304713a4b65faa6d0f9a70")>
    <CalculationSpecification(Id:="P294a_InYearTotalOtherFactors_NoExc", Name:="P294a_InYearTotalOtherFactors_NoExc")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="b2e46eab20374cd1864365a94a7ab9b3", Name:="Totals")>
    Public Function P294a_InYearTotalOtherFactors_NoExc As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="4fb316af17764011a16f143a2551f44e")>
    <CalculationSpecification(Id:="P295_Dedelegation", Name:="P295_Dedelegation")>
    <PolicySpecification(Id:="93f568b56656481ab43ac14119890c7f", Name:="School Budget Share")>
    <PolicySpecification(Id:="b2e46eab20374cd1864365a94a7ab9b3", Name:="Totals")>
    Public Function P295_Dedelegation As Decimal
        Return Decimal.MinValue
    End Function

    <Calculation(Id:="330ed1c2ff1e44c3bbb421ee9d0bc132")>
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

    <Calculation(Id:="eece23d4308f43aab8ecddc4fc8abb8b")>
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

    <Calculation(Id:="11cfb9b7c2b24ce28a7cd49484f4a999")>
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

    <Calculation(Id:="9a1787e092ce40f6855a0d39ed5c87a4")>
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

    <Calculation(Id:="a42319586a2949ba8767a98bfe748afa")>
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

    <Calculation(Id:="cbcfd455b7484d4dabadc463b7791fcc")>
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

    <Calculation(Id:="8f26acd4097640638ef99efaa9856c8f")>
    Public Function F400_HN_Academies As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        If currentscenario.periodid = 2017181 And (F100_AllAcademies = 17181 Or F100_AllAcademies = 17182 Or F100_AllAcademies = 17183) Then
            Result = 1
        Else
            Exclude(rid)
        End if

        Return result
    End Function

    <Calculation(Id:="f77c61d3fae647a78236d30f61ff5904")>
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

    <Calculation(Id:="9e1eb112df3140c78cc635091e4777d5")>
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

    <Calculation(Id:="24dec1d353cd47c0a14d2bdf678c3b12")>
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

    <Calculation(Id:="4c2bf18a5c22420ca5f7c9f1a194fd11")>
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

    <Calculation(Id:="d1709efb30744fd9a9d6d633a958b450")>
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

    <Calculation(Id:="aa1ff590d2634a1ca1cc15ea959830e0")>
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

    <Calculation(Id:="9f29098041d9455086decb402962f26a")>
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

    <Calculation(Id:="db7a9681578a44f69a9875f8a22a78ec")>
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

    <Calculation(Id:="80eed109a8604102983a644d8e5627a5")>
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

    <Calculation(Id:="7fd074dc3e0c48fa8b913136918498be")>
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

    <Calculation(Id:="a1a383d5f72247048befdcf6a189e94d")>
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

    <Calculation(Id:="a036c2d8924d4e168dbad9fe9b545b68")>
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

    <Calculation(Id:="d6a6d3df9a0043b78b471bbb7cae99f5")>
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

    <Calculation(Id:="0180e6352d9444839726b301dad059d1")>
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

    <Calculation(Id:="2af7e1c98dce446ba5a8b73e5a907ee7")>
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

    <Calculation(Id:="2e05fd81fd904222ac4cec4e4baca548")>
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

    <Calculation(Id:="abd84ba720a54861961d4edc6aae0bd7")>
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

    <Calculation(Id:="d8ef611f3a0b44fa807c43cc94170c50")>
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

    <Calculation(Id:="caf5491a75bd4c69b4b25783dbd3abe4")>
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

    <Calculation(Id:="4bf46c306b3a4693abca4c5ab991881c")>
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

    <Calculation(Id:="d16cdb04b8ec494483e09366fc54b94c")>
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

    <Calculation(Id:="6b0355c147a1472897cca8f9d6766249")>
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

    <Calculation(Id:="4cabdc29e6e14203843429fd5a847fbe")>
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

    <Calculation(Id:="45876a1c29604a0ebae8102669b4f5e6")>
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

    <Calculation(Id:="d9acafd535e1499b852742ee241c3ec0")>
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

    <Calculation(Id:="e1cafcb3b27146f3af7564984c2636d9")>
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

    <Calculation(Id:="0bc9951f23034168856ef6ae18af3bdd")>
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

    <Calculation(Id:="f66360fce5d34be1a2d6340c90d33c07")>
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

    <Calculation(Id:="da57c502c0584b31bf8bc18ba8f5b8e3")>
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

    <Calculation(Id:="cf2013e2aaee49519a13810fe79fa310")>
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

    <Calculation(Id:="63dbf96ff8894c36aa5eba47d3fb265e")>
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

    <Calculation(Id:="941161fd516546e0a4b9f4ec3af6c28e")>
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

    <Calculation(Id:="e06a7f3ea7a1496697970d0a2f862797")>
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

    <Calculation(Id:="05f158c8ba3245d2b24bdb4a66b8ba9f")>
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

    <Calculation(Id:="ffec8031f801410f80ce1f9bb0a28dfd")>
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

    <Calculation(Id:="c4c4f9bde90349b6990a40ab7d01955c")>
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

    <Calculation(Id:="f318b8e8fc4e408e910b3fff350704c6")>
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

    <Calculation(Id:="e1ad8bfc8ab8445c9900b737de62bc7d")>
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

    <Calculation(Id:="1075fbf2894b4936aed9704f05f72d7c")>
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

    <Calculation(Id:="21d6eeba1c494208a98a2d5401e45df8")>
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

    <Calculation(Id:="57e9d4e82bdb4e12bc4f4786f1c43978")>
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

    <Calculation(Id:="d4e20a53ec5346938454b1cbcdfc6296")>
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

    <Calculation(Id:="fe46dd5d8ff2428e84120bf6b06cfc16")>
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

    <Calculation(Id:="807d54cf206f462cb15481ad30788e32")>
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

    <Calculation(Id:="ef6cbdd63a5340d69ab7b540e0fc4b62")>
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

    <Calculation(Id:="bdd45ec8981d4e2ab07874d3d3783d9d")>
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

    <Calculation(Id:="7b04e8405e994153a1a1dbf0c851752a")>
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

    <Calculation(Id:="a793c05b8f104183b13f4f532cb88133")>
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

    <Calculation(Id:="1dfeb5071e9a4f4f8adbe123831100b3")>
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

    <Calculation(Id:="032e7764e9574a5abd597030aebc4c46")>
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

    <Calculation(Id:="3fcf332003a44aeda490aaa408cf4a89")>
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

    <Calculation(Id:="134bf8ff68c04673b77c69a1ca08faae")>
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

    <Calculation(Id:="97b0de5c88ad43ba8e788d634d6af8f1")>
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

    <Calculation(Id:="47333fcf7b15476c9f0dd70deacfe1af")>
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

    <Calculation(Id:="da535ad9402645e38999a8d03bdc2525")>
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

    <Calculation(Id:="45d68435c56e42da82558568bb5d3525")>
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

    <Calculation(Id:="97b32db9a6474b7aad4f55f15c6da119")>
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

    <Calculation(Id:="8d800c53104942ca9c0817183f2cadf6")>
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

    <Calculation(Id:="1c976adf17cf405e9bc5e23c9a562237")>
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

    <Calculation(Id:="ba1060bc97f04baaad466d5400fec3f4")>
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

    <Calculation(Id:="742659a3536c4d59a062f9ddcb05f22b")>
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

    <Calculation(Id:="bf49a249caa54555a4a267ee2845ac31")>
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

    <Calculation(Id:="757ac36421e74d6dbedf28f290ff6ced")>
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

    <Calculation(Id:="27465d9bec20475cb78392f9e320ae91")>
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

    <Calculation(Id:="1c7e918dab904a3c8437b52203fc25aa")>
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

    <Calculation(Id:="e50be8f811d2465d98c532b9c1fd34d9")>
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

    <Calculation(Id:="406764ce8d404541a2e0639774cd0aa1")>
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

    <Calculation(Id:="b3bf1a42093d484fa70651eba2fd744f")>
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

    <Calculation(Id:="cfcc4a0a6ffd45b990d4f0a74ab84bef")>
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

    <Calculation(Id:="b863cd148ee34815b641281905354c34")>
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

    <Calculation(Id:="6d1a91a3d8c4458581a6bceff7efd785")>
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

    <Calculation(Id:="47163cd02ac7490eb8da1e77914e8c30")>
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

    <Calculation(Id:="1895ebafa4024e52ac2f781be98a53e2")>
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

    <Calculation(Id:="5d0de68c0502474199b6f2330166c708")>
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

    <Calculation(Id:="28d60365556f47f6a119d973753ab3ae")>
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

    <Calculation(Id:="ec6fa32d979a4066833915e3226b012b")>
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

    <Calculation(Id:="2338ca91d6c24ce1a63a4344b74b8ed1")>
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

    <Calculation(Id:="9b8e74c67a7a4de99175dec35323e606")>
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

    <Calculation(Id:="b0b7709eb43a40958fabba1cedaeea55")>
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

    <Calculation(Id:="af2a5645ad864b948681b4576c32a747")>
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

    <Calculation(Id:="8adca09561ab4f848abe3415bc71ae36")>
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

    <Calculation(Id:="81ffc013c9f14b598a308ddef9553632")>
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

    <Calculation(Id:="5f9a7a48f0f0408187e2e3dd6ccd12fa")>
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

    <Calculation(Id:="15730c77abca425bab8fbb41f7acf0df")>
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

    <Calculation(Id:="de83a2a070ac4d52b68a0969fd618036")>
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

    <Calculation(Id:="b3b82064f6d24da1ad1e2b254221205e")>
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

    <Calculation(Id:="230370803f3e4d0aab6dcacd9fe00e28")>
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

    <Calculation(Id:="3f2cfc74c87d4282a809ea637fd16685")>
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

    <Calculation(Id:="245352abcd274264885eae3a890ae87a")>
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

    <Calculation(Id:="0a7bdccbf1d74f52a826573a46c0fcd6")>
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

    <Calculation(Id:="de038706fe344eb3959e7254fb734e7d")>
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

    <Calculation(Id:="1336081427ab4708abc75d5e2cfb13c4")>
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

    <Calculation(Id:="eef88227a0fe48be9c660ab01325ed88")>
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

    <Calculation(Id:="4ba7f36bb8bb41978d2037d65ccc404f")>
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

    <Calculation(Id:="8b8cad566f654a40b3f7adbbabadc795")>
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

    <Calculation(Id:="117ba53b33b34915bd025e9b4fb716db")>
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

    <Calculation(Id:="5197fe55d5394b12a559201751bf259b")>
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

    <Calculation(Id:="9669858fafc6469eb8f90df91d856695")>
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

    <Calculation(Id:="f349a781317f46f2b9aad61d302b26d2")>
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

    <Calculation(Id:="aad69e8f77064702a100b224c701756d")>
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

    <Calculation(Id:="f409a60f2f7c45baa873d123c79ac80a")>
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

    <Calculation(Id:="8b69aa2dd1654253a57790a11b61d24e")>
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

    <Calculation(Id:="90335ccfa38a465eac9fdfee0d81f599")>
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

    <Calculation(Id:="8386adbc7a4f46eb96d984430a885c7c")>
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

    <Calculation(Id:="b78bbd341fec4cccb01c43f126f43b0e")>
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

    <Calculation(Id:="56d5dd0205dc40eeb44ee63c42ff6256")>
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

    <Calculation(Id:="ca15e90ae88d49eaaa3eded325993650")>
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

    <Calculation(Id:="1cfda83a6a814bf5a522e79eea9bd8e8")>
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

    <Calculation(Id:="8e9bd6e23df345e0b82198bcae1ad558")>
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

    <Calculation(Id:="9ea684282b0a435e93a5535b30271d44")>
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

    <Calculation(Id:="00a3a37a15564f0990e61d4a39cf810f")>
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

    <Calculation(Id:="ae433b4c79214bff8baeaa80444e74f5")>
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

    <Calculation(Id:="13ba3ce2763a43b2b6d98e9b68a33f2f")>
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

    <Calculation(Id:="48fccbf256ef427985cbea090f67d3c5")>
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

    <Calculation(Id:="20337121eba34c11865d822fb8f4abbc")>
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

    <Calculation(Id:="1bcf4eff72df473f913b5fee31dd1d8e")>
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

    <Calculation(Id:="a9c41514f3104add9c5c2e67752896bb")>
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

    <Calculation(Id:="3d5fdd0ab9b543e5a2c800b346af0a6f")>
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

    <Calculation(Id:="a031e5f374c0429faa036215910adde5")>
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

    <Calculation(Id:="b3e2598019684769867b262ea8da7443")>
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

    <Calculation(Id:="b67318d9bdef4aaaa6d69e887c35fbd1")>
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

    <Calculation(Id:="4940b4ec0410433a8a856f5c6cf109df")>
    Public Function P002_NOR_Est_Y1to4 As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim Y1 As Decimal = Datasets.EstimateNumberCounts.EstNORY1
        Dim Y2 As Decimal = Datasets.EstimateNumberCounts.EstNORY2
        Dim Y3 As Decimal = Datasets.EstimateNumberCounts.EstNORY3
        Dim Y4 As Decimal = Datasets.EstimateNumberCounts.EstNORY4
        result =(Y1 + Y2 + Y3 + Y4)
        Return result
    End Function

    <Calculation(Id:="4b7dfa7af5ea464eadb8b2a88b1053f0")>
    Public Function P003_NOR_Est_Y5to6 As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim Y5 As Decimal = Datasets.EstimateNumberCounts.EstNORY5
        Dim Y6 As Decimal = Datasets.EstimateNumberCounts.EstNORY6
        Result = Y5 + Y6
        Return result
    End Function

    <Calculation(Id:="f590388352a74c0bbb80e2579ffa279f")>
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

    <Calculation(Id:="0d818fd26b5e46df94fe2a472321c42c")>
    Public Function P005_NOR_Est_Y8to11 As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim Y8 As Decimal = Datasets.EstimateNumberCounts.EstNORY8
        Dim Y9 As Decimal = Datasets.EstimateNumberCounts.EstNORY9
        Dim Y10 As Decimal = Datasets.EstimateNumberCounts.EstNORY10
        Dim Y11 As Decimal = Datasets.EstimateNumberCounts.EstNORY11
        result =(Y8 + Y9 + Y10 + Y11)
        Return result
    End Function

    <Calculation(Id:="c9cad06bbf9443ba8f57c4024a90e3c6")>
    Public Function P006_NOR_Est_KS3 As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim Y7 As Decimal = Datasets.EstimateNumberCounts.EstNORY7
        Dim Y8 As Decimal = Datasets.EstimateNumberCounts.EstNORY8
        Dim Y9 As Decimal = Datasets.EstimateNumberCounts.EstNORY9
        result =(Y7 + Y8 + Y9)
        Return result
    End Function

    <Calculation(Id:="674221b828994fa388e6be6915b2920e")>
    Public Function P007_NOR_Est_KS4 As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim Y10 As Decimal = Datasets.EstimateNumberCounts.EstNORY10
        Dim Y11 As Decimal = Datasets.EstimateNumberCounts.EstNORY11
        result = Y10 + Y11
        Return result
    End Function

    <Calculation(Id:="e1ea8ba538a841538c79197c945a8e82")>
    Public Function P008_NOR_Est_RtoY11 As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim Pri As Decimal = P001_NOR_Est_Pri
        Dim Sec As Decimal = P004_NOR_Est_Sec
        result = Pri + Sec
        Return result
    End Function

    <Calculation(Id:="f92bee50ed8249fd812a0040b57981ee")>
    Public Function P009_NOR_Est_Y12toY14 As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim Y12 As Decimal = Datasets.EstimateNumberCounts.EstNORY12
        Dim Y13 As Decimal = Datasets.EstimateNumberCounts.EstNORY13
        Dim Y14 As Decimal = Datasets.EstimateNumberCounts.EstNORY14
        result = Y12 + Y13 + Y14
        Return result
    End Function

    <Calculation(Id:="be59e53b1fc94fd78c67077931e73bf7")>
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

    <Calculation(Id:="026827bc4a2f4baabf92794909f51b94")>
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

    <Calculation(Id:="c1a70d64b07941e9a0259d4d9fbded87")>
    Public Function P012_NOR_Total_YG_R_Y11 As Decimal
        Dim result As Decimal = 0 'change to As String if text product
        Dim P010 As Decimal = P010_NOR_TotalPri_YG
        Dim P011 As Decimal = P011_NOR_TotalSec_YG
        result = P010 + P011
        Return result
    End Function

    <Calculation(Id:="d16efe2d917548c5bf94b3ccaa8c5a72")>
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

    <Calculation(Id:="b8917232dba941d7ad91c73aa462cbda")>
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

    <Calculation(Id:="4e9ae8b8bb3849a3a327da4c573baff5")>
    Public Function NOR_P24_Total_NOR_KS4_SBS As Decimal
        Dim result = Decimal.Zero
        If currentscenario.periodid = 2017181 And (F100_AllAcademies = 17182 Or F100_AllAcademies = 17183) And F900_FundingBasis = 1 Then
            Result = NOR_P08_KS4
        Else
            Result = NOR_P08_KS4 - NOR_P24b_Actual_HN_KS4_Deducted
        End If

        Return result
    End Function

    <Calculation(Id:="e5e53568ecb5492086964f9385228e13")>
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

    <Calculation(Id:="7baf62bcee7c4326beb5330e84ebd713")>
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

    <Calculation(Id:="c666224a2f054860a9b218e7a2a0ef83")>
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

    <Calculation(Id:="226944ec8ebb4706a4015d99f09091eb")>
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

    <Calculation(Id:="5cbd7f602d594caa92f7ccc01b64e02f")>
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

    <Calculation(Id:="322409f8d1044249aab6506394802d85")>
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

    <Calculation(Id:="fa619378a574494faba1b0330eb2d951")>
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

    <Calculation(Id:="3f3c0b1bf2924c9aa28a47fda77af71b")>
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

    <Calculation(Id:="32a5d31b01d946ab85e26ad6b90f9904")>
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

    <Calculation(Id:="bcbc1c478015471bbfbd46ca932f8554")>
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

    <Calculation(Id:="4fb177a4452e4a928e84af16df18fab2")>
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

    <Calculation(Id:="639c664d894c46ce817d75b983bfdd28")>
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

    <Calculation(Id:="08a0cff27f014fee82d03e8ca707758d")>
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

    <Calculation(Id:="ed30ad00e1f54c4c978c3aea145144bc")>
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

    <Calculation(Id:="c4d97d7e0baf421aa8368ac7d813afbc")>
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

    <Calculation(Id:="5bb7a7de009947b39b110935e5e0d7b9")>
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

    <Calculation(Id:="4dac52c8a1104c41be4376eca7f3ef23")>
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

    <Calculation(Id:="6392c06444ab4d17baa26e0e032bfda5")>
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

    <Calculation(Id:="cdc066b92bfd4bb8b93794dd61106e5d")>
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

    <Calculation(Id:="1626478568cd4c6990dc4bac40f45983")>
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

    <Calculation(Id:="09cb83ed84d445c7b4ed12db5febaec1")>
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

    <Calculation(Id:="a07a731abb7b4b61b4310f0dbbe6bc1c")>
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

    <Calculation(Id:="905b2ba29e804d078b76bbe442a62306")>
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

    <Calculation(Id:="9c4a100984dc44649591a056c2bde49e")>
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

    <Calculation(Id:="fb1d0bebaf6147b696389aca67b34f33")>
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

    <Calculation(Id:="5265138f45da48f1be70073de316b813")>
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

    <Calculation(Id:="6232a2a7f1df40399d20cb080b89d0f3")>
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

    <Calculation(Id:="0b0848abff9e4c65a0ce1858a908ed2a")>
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

    <Calculation(Id:="b8e975ad6b1843ef8b74f0f6956371e8")>
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

    <Calculation(Id:="62242d1dac6e4dbe90c2203c081941b5")>
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

    <Calculation(Id:="5f2a3b4f691d4b6ab8a28f557e6712a4")>
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

    <Calculation(Id:="8f4de85fbfd24e57b7c13d9b55b2adaf")>
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

    <Calculation(Id:="0e719fd770084fdd86ad0dea7bcf9e7b")>
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

    <Calculation(Id:="6ac768c36f1642abb55accfcab192c37")>
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

    <Calculation(Id:="e076847d3c804b8cb1bd661f43c641b4")>
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

    <Calculation(Id:="4298b72df31b47198797b76dc356e871")>
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
