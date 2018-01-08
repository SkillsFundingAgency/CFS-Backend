Option Strict Off
Option Explicit On

Imports System
Imports System.Text
Imports System.Collections.ObjectModel
Imports System.Text.RegularExpressions
Imports System.Collections.Generic
Imports System.Linq
Imports System.Data
Imports System.Windows
Imports DfE.Controls
Imports DfE.Diagnostics
Imports DfE.Modeling
Imports DfE.ModelingEngine
Imports DfE.Models
Imports DfE.Models.Modeling
Imports DfE.Models.Datasets
Imports Microsoft.VisualBasic

Public Module ModelingCalcModule

 Public Function GetProductResult_7582(rid As String) As Decimal
Dim __key = "7582" + rid
If (_engine._productResultCache.ContainsKey(__key)) = True Then Return _engine._productResultCache(__key)

 Dim result = 0
 


 
 Dim providersubtype As String = _engine.GetDsDataValue(rid, 9074)
 Dim previouslymaintained As Boolean = _engine.GetDsDataValue(rid, 11178)
 Dim openingdate As Date = _engine.GetDsDataValue(rid, 9077)
 Dim convertdate As Date = _engine.GetDsDataValue(rid, 73189)
 
 
 Dim budgetID1718 As String = _engine.GetDsDataValue(rid, 9429, "2017181")
 Dim fundingbasis1718 As String = _engine.GetDsDataValue(rid, 11187, "2017181")
 
 
 Dim budgetID1617 As String = _engine.GetDsDataValue(rid, 9429, "2016171")
 Dim fundingbasis1617 As String = _engine.GetDsDataValue(rid, 11187, "2016171") 
 
 
 Print(providersubtype, "providersubtype", rid, 7582)
 Print(previouslymaintained, "previouslymaintained", rid, 7582)
 Print(openingdate, "openingdate", rid, 7582)
 Print(convertdate, "convertdate", rid, 7582)
 Print(budgetID1718, "budgetID1718", rid, 7582)
 Print(fundingbasis1718, "fundingbasis1718", rid, 7582)
 Print(budgetID1617, "budgetID1617", rid, 7582)
 Print(fundingbasis1617, "fundingbasis1617", rid, 7582) 


 
 
 
 
 
 
 
 
 

 If providersubtype = "14CTC" Then
 Exclude(RID, 7582)
 Else
 
 If currentscenario.periodid = 2017181 Then
 If String.IsNullOrEmpty(budgetID1718) Or String.IsNullOrEmpty(fundingbasis1718) Then
 Exclude(RID, 7582) 
 Else
 If convertdate >= openingdate And BudgetID1718.Contains("ZCONV") Or openingdate >= "01 September 2017" And BudgetID1718.Contains("YPLRE") Then
 result = 17183 
 Else
 
 If ((BudgetID1718.Contains("YPLRE") Or BudgetID1718.Contains("ZCONV")) _
 Or ((BudgetID1718.Contains("YPLRC") Or BudgetID1718.Contains("YPLRA") Or BudgetID1718.Contains("YPLRD")) And previouslymaintained = True)) And (openingdate > "01 January 0001" And openingdate < "01 April 2017") Then 
 
 
 
 result = 17181
 
 Else 
 If openingdate >= "01 April 2017" And openingdate < "01 September 2017" Then 
 result = 17182
 
 End if
 End if
 End If 
 End If
 
 
 Else
 If currentscenario.periodid = 2016171 Then
 If String.IsNullOrEmpty(budgetID1617) Or String.IsNullOrEmpty(fundingbasis1617) Then
 Exclude(RID, 7582)
 Else
 If (BudgetID1617.Contains("YPLRE") And Openingdate >= "01 April 2017" And Openingdate < "01 September 2017") _
 Or (BudgetID1617.Contains("ZCONV") And convertdate >= "01 April 2017" And convertdate < "01 September 2017") _
 Or ((BudgetID1617.Contains("YPLRC") Or BudgetID1617.Contains("YPLRA") Or BudgetID1617.Contains("YPLRD") _
 And (Openingdate >= "01 April 2017" And Openingdate < "01 September 2017") Or (convertdate >= "01 April 2017" And convertdate < "01 September 2017")) And previouslymaintained = True) Then

 result = 16171
 Else 
 Exclude(RID, 7582)
 End If
 End If
 End If
 End If 
 
 End If
 
 
 
 
_engine._productResultCache.Add(__key, result)
Return result
End Function

 Public Function GetProductResult_7708(rid As String) As Decimal
Dim __key = "7708" + rid
If (_engine._productResultCache.ContainsKey(__key)) = True Then Return _engine._productResultCache(__key)

 Dim result = 0
 
 Dim IY_Filter As Decimal = GetProductResult_7582(rid) 
 Dim Funding_Basis As String = _engine.GetDsDataValue(rid, 11187, "2017181")
 Dim Budget_ID As String = _engine.GetDsDataValue(rid, 9429, "2017181") 
 Dim Convert_date As Date = _engine.GetDsDataValue(rid, 73189)
 Dim Dateopened As Date = _engine.GetDsDataValue(rid, 9077)
 
 If String.IsNullOrEmpty(Budget_ID) Then
 Exclude(RID, 7708) 
 Else If currentscenario.periodid = 2017181 And (IY_Filter = 17181 Or IY_Filter = 17182 Or IY_Filter = 17183) then
 If Budget_ID.Contains("YPLRE") Then 
 result = 1
 Else result = 0
 End If
 Else Exclude(rid, 7708) 
 End If 
 
 Print(Convert_date, "Convert Date", rid, 7708)
 Print(Dateopened, "Open Date", rid, 7708)
 Print(Funding_Basis , "FundingBasis", rid, 7708)
 Print(Budget_ID, "Budget ID", rid, 7708)
 
 
_engine._productResultCache.Add(__key, result)
Return result
End Function

Public Function GetProductResult_10458(rid As String) As Decimal
 Dim result As Decimal = 0
 
Dim __key = "10458" + rid
If (_engine._productResultCache.ContainsKey(__key)) = True Then Return _engine._productResultCache(__key)

 
 Dim EstNOR_Rep As Decimal = _engine.GetDsDataValue(rid, 87272)
 Dim EstNOR_Y1 As Decimal = _engine.GetDsDataValue(rid, 87274)
 Dim EstNOR_Y2 As Decimal = _engine.GetDsDataValue(rid, 87276)
 Dim EstNOR_Y3 As Decimal = _engine.GetDsDataValue(rid, 87278)
 Dim EstNOR_Y4 As Decimal = _engine.GetDsDataValue(rid, 87280)
 Dim EstNOR_Y5 As Decimal = _engine.GetDsDataValue(rid, 87282)
 Dim EstNOR_Y6 As Decimal = _engine.GetDsDataValue(rid, 87284)
 
 Print(EstNOR_Rep,"Reception",rid, 10458)
 Print(EstNOR_Y1,"Year 1",rid, 10458)
 Print(EstNOR_Y2,"Year 2",rid, 10458)
 Print(EstNOR_Y3,"Year 3",rid, 10458)
 Print(EstNOR_Y4,"Year 4",rid, 10458)
 Print(EstNOR_Y5,"Year 5",rid, 10458)
 Print(EstNOR_Y6,"Year 6",rid, 10458)
 
 
 Result = (EstNOR_Rep + EstNOR_Y1 + EstNOR_Y2 + EstNOR_Y3 + EstNOR_Y4 + EstNOR_Y5 + EstNOR_Y6) 
 
 
_engine._productResultCache.Add(__key, result)
Return result
 
End Function
 

 Public Function GetProductResult_7594(rid As String) As Decimal
Dim __key = "7594" + rid
If (_engine._productResultCache.ContainsKey(__key)) = True Then Return _engine._productResultCache(__key)

 Dim result = 0
 
 
 
 Dim Date_Opened as date = _engine.GetDsDataValue(rid, 9077)
 Dim Budget_ID as String = _engine.GetDsDataValue(rid, 9429, "2017181")
 Dim ProviderType as string = _engine.GetDsDataValue(rid, 9072)
 Dim Funding_Basis as String = _engine.GetDsDataValue(rid, 11187, "2017181")
 

 
 
 
 Print(Funding_Basis, "ABCD Type",rid, 7594)
 
 
 
 If Budget_ID isnot nothing then
 
 
 If currentscenario.periodid = 2017181 and Funding_Basis = "Census" and (Budget_ID.Contains("YPLRE") Or Budget_ID.Contains("ZCONV")) Then
 Result = 1 
 ElseIf currentscenario.periodid = 2017181 and Funding_Basis = "Estimate" and (Budget_ID.Contains("YPLRE") Or Budget_ID.Contains("ZCONV")) Then
 Result = 2 
 ElseIf currentscenario.periodid = 2017181 and (Funding_Basis = "Place") and (Budget_ID.Contains("YPLRE") Or Budget_ID.Contains("ZCONV")) Then
 Result = 3 
 Else
 Exclude(rid, 7594) 
 End If
 Else 
 Exclude(rid, 7594)
 
 End If
 
 
 
_engine._productResultCache.Add(__key, result)
Return result
End Function


Public Function GetProductResult_7700(rid As String) As Decimal
Dim __key = "7700" + rid
If (_engine._productResultCache.ContainsKey(__key)) = True Then Return _engine._productResultCache(__key)

 Dim Result = 0
 
 
 
 
 
 Dim RU_Applicable As String = LAtoProv(_engine.GetLaDataValue(rid, 86802, Nothing))

 Print(RU_Applicable, "Recep Uplift",rid, 7700)
 
 If RU_Applicable = "Yes" Then
 Result = 1
 Else If RU_Applicable = "NO" Then
 Result = 0
 
 End If
 
 If Result = 0 Then
 Exclude(rid, 7700)
 End If
 
 
_engine._productResultCache.Add(__key, Result)
Return Result
 
 
 
 
End Function


Public Function GetProductResult_10459(rid As String) As Decimal
 Dim result As Decimal = 0
 
Dim __key = "10459" + rid
If (_engine._productResultCache.ContainsKey(__key)) = True Then Return _engine._productResultCache(__key)

 Dim EstNOR_Y7 As Decimal = _engine.GetDsDataValue(rid, 87286)
 Dim EstNOR_Y8 As Decimal = _engine.GetDsDataValue(rid, 87288)
 Dim EstNOR_Y9 As Decimal = _engine.GetDsDataValue(rid, 87290)
 Dim EstNOR_Y10 As Decimal = _engine.GetDsDataValue(rid, 87292)
 Dim EstNOR_Y11 As Decimal = _engine.GetDsDataValue(rid, 87294)
 
 Print(EstNOR_Y7,"Year 7",rid, 10459)
 Print(EstNOR_Y8,"Year 8",rid, 10459)
 Print(EstNOR_Y9,"Year 9",rid, 10459)
 Print(EstNOR_Y10,"Year 10",rid, 10459)
 Print(EstNOR_Y11,"Year 11",rid, 10459)
 
 Result = (EstNOR_Y7 + EstNOR_Y8 + EstNOR_Y9 + EstNOR_Y10 + EstNOR_Y11)
 
 
 
 
_engine._productResultCache.Add(__key, result)
Return result
 
End Function


Public Function GetProductResult_10460(rid As String) As Decimal
 Dim result As Decimal = 0
 
Dim __key = "10460" + rid
If (_engine._productResultCache.ContainsKey(__key)) = True Then Return _engine._productResultCache(__key)

 Dim Primary As Decimal = GetProductResult_10458(rid) 
 Dim Secondary As Decimal = GetProductResult_10459(rid) 
 
 Print(Primary,"Primary",rid, 10460)
 Print(Secondary,"Secondary",rid, 10460)
 
 Result = (Primary + Secondary)
 
 
_engine._productResultCache.Add(__key, result)
Return result
 
End Function


Public Function GetProductResult_7645(rid As String) As Decimal
Dim __key = "7645" + rid
If (_engine._productResultCache.ContainsKey(__key)) = True Then Return _engine._productResultCache(__key)

 Dim result = 0
 
 
 
 
 
 
 
 
 Dim RU_Flag as Single = LAtoProv(GetProductResult_7700(rid) )
 Dim IsNull As Boolean
 IsNull = IIf(_engine.GetApprovedDsDataValue(rid, 86010), false, true) 
 Dim RU_InpAdj As Decimal = _engine.GetDsDataValue(rid, 86010)
 Dim RU_NOR_APT As Decimal = _engine.GetDsDataValue(rid, 86284)
 Dim RU_NOR_Census As Decimal = _engine.GetDsDataValue(rid, 87330) 
 Dim RU_NOR_RFDC As Decimal = _engine.GetDsDataValue(rid, 87266) 
 Dim AcadFilter As Decimal = GetProductResult_7582(rid) 
 Dim Fund_Basis As Decimal = GetProductResult_7594(rid) 
 Dim Guaranteed_Numbers As String = _engine.GetDsDataValue(rid, 88442) 
 Dim InpAdj_NOR As Decimal = _engine.GetDsDataValue(rid, 85996) 
 Dim RFDC_NOR As Decimal = GetProductResult_10460(rid)  
 
 Print(AcadFilter, "Filter", rid, 7645)
 Print(RU_Flag, "RU_Flag", rid, 7645)
 Print(Fund_Basis, "Funding Basis", rid, 7645) 
 Print(RU_InpAdj, "Inp And Adj RU",rid, 7645)
 Print(Guaranteed_Numbers, "Guaranteed_Numbers",rid, 7645)
 Print(InpAdj_NOR, "InpAdj_NOR",rid, 7645)
 Print(RFDC_NOR, "RFDC_NOR",rid, 7645) 
 
If currentscenario.periodid = 2017181 And (AcadFilter = 17181) Then 
 If RU_Flag = 1 And Fund_Basis = 1 And IsNull = False Then 
 Result = RU_InpAdj
 Else If RU_Flag = 1 And Fund_Basis = 1 And IsNull = True Then
 Result = RU_NOR_Census
 Else If (RU_Flag = 1 And Fund_Basis = 2 And Guaranteed_Numbers = "Yes" And InpAdj_NOR > RFDC_NOR) Then
 Result = RU_InpAdj
 Else If RU_Flag = 1 And Fund_Basis = 2 Then
 Result = RU_NOR_RFDC
 End If 
 Else If currentscenario.periodid = 2017181 And (AcadFilter = 17182 Or AcadFilter = 17183) Then 
 If RU_Flag = 1 And Fund_Basis = 1 Then
 Result = RU_NOR_APT
 Else If (RU_Flag = 1 And Fund_Basis = 2 And Guaranteed_Numbers = "Yes" And InpAdj_NOR > RFDC_NOR) Then
 Result = RU_InpAdj
 Else If RU_Flag = 1 And Fund_Basis = 2 Then
 Result = RU_NOR_RFDC
 End If 
 Else Exclude(rid, 7645) 
End If 
 
 
_engine._productResultCache.Add(__key, result)
Return result 
 
 End Function
 

Public Function GetProductResult_7646(rid As String) As Decimal
Dim __key = "7646" + rid
If (_engine._productResultCache.ContainsKey(__key)) = True Then Return _engine._productResultCache(__key)

 Dim result = 0
 
 
 
 
 
 Dim DateOpened As Date = _engine.GetDsDataValue(rid, 9077)
 Dim IsNull As Boolean
 IsNull = IIf(_engine.GetApprovedDsDataValue(rid, 85998), false, true) 
 Dim NOR_Inp_Adj As Decimal = _engine.GetDsDataValue(rid, 85998) 
 Dim NOR_Pri_Cen As Decimal = _engine.GetDsDataValue(rid, 87318)
 Dim NOR_Pri_Est As Decimal = GetProductResult_10458(rid) 
 Dim NOR_Pri_APT As Decimal = _engine.GetDsDataValue(rid, 86272)
 Dim AcadFilter As Decimal = GetProductResult_7582(rid) 
 Dim FundingBasis As String = GetProductResult_7594(rid) 
 Dim APT_PLaces As Decimal = _engine.GetDsDataValue(rid, 86367)
 Dim NOR_RU As Decimal = GetProductResult_7645(rid) 
 Dim NOR_Pri As Decimal 
 Dim Guaranteed_Numbers As string = _engine.GetDsDataValue(rid, 88442) 
 Dim InpAdj_NOR As Decimal = _engine.GetDsDataValue(rid, 85996) 
 Dim Est_NOR As Decimal = GetProductResult_10460(rid) 
 
 Print(IsNull, "InpAdj null check", rid, 7646)
 Print(NOR_Inp_Adj, "NOR InpAdj", rid, 7646)
 Print(DateOpened, "Date Opened", rid, 7646)
 Print(FundingBasis, "funding basis", rid, 7646)
 Print(NOR_Pri_Cen, "NOR_Pri_Cen", rid, 7646) 
 Print(NOR_Pri_Est, "NOR_Pri_Est", rid, 7646) 
 Print(NOR_Pri_APT, "NOR Pri APT", rid, 7646)
 Print(AcadFilter, "Acad Filter 1718", rid, 7646) 
 Print(Guaranteed_Numbers, "Guaranteed_Numbers",rid, 7646)
 Print(InpAdj_NOR, "InpAdj_NOR",rid, 7646) 
 Print(Est_NOR, "Est_NOR",rid, 7646) 
 
If currentscenario.periodid = 2017181 And AcadFilter = 17181 then
 If FundingBasis = 1 And IsNull = False Then
 NOR_Pri = NOR_Inp_Adj
 ElseIf FundingBasis = 1 And IsNull = True Then
 NOR_Pri = NOR_Pri_Cen 
 ElseIf (FundingBasis = 2 And Guaranteed_Numbers = "Yes" And InpAdj_NOR > Est_NOR) Then
 NOR_Pri = NOR_Inp_Adj
 ElseIf FundingBasis = 2 Then
 NOR_Pri = NOR_Pri_Est 
 End If
 ElseIf currentscenario.periodid = 2017181 And (AcadFilter = 17182 Or AcadFilter = 17183) Then
 If FundingBasis = 1 Then
 NOR_Pri = NOR_Pri_APT + APT_PLaces - NOR_RU
 ElseIf (FundingBasis = 2 And Guaranteed_Numbers = "Yes" And InpAdj_NOR > Est_NOR) Then
 NOR_Pri = NOR_Inp_Adj
 ElseIf FundingBasis = 2 Then
 NOR_Pri = NOR_Pri_Est 
 End If 
 Else Exclude(rid, 7646) 
End If 
 
 result = NOR_Pri
 
 
_engine._productResultCache.Add(__key, result)
Return result
End Function


Public Function GetProductResult_7653(rid As String) As Decimal
Dim __key = "7653" + rid
If (_engine._productResultCache.ContainsKey(__key)) = True Then Return _engine._productResultCache(__key)

 Dim result = 0
 
 
 
 
 Dim HNPri As Decimal = _engine.GetDsDataValue(rid, 86367)
 Dim AcadFilter As Decimal = GetProductResult_7582(rid) 
 
 Print(HNPri,"HNPri", rid, 7653)
 Print(AcadFilter,"Academy Filter", rid, 7653)
 
If currentscenario.periodid = 2017181 And (AcadFilter = 17181 Or AcadFilter = 17182 Or AcadFilter = 17183) then 
 result = HNPri 
Else Exclude(rid, 7653) 
End if 
 
 
_engine._productResultCache.Add(__key, result)
Return result
End Function


Public Function GetProductResult_12257(rid As String) As Decimal
 Dim result As Decimal = 0
 
Dim __key = "12257" + rid
If (_engine._productResultCache.ContainsKey(__key)) = True Then Return _engine._productResultCache(__key)

 Dim EstNOR_Y7 As Decimal = _engine.GetDsDataValue(rid, 87286)
 Dim EstNOR_Y8 As Decimal = _engine.GetDsDataValue(rid, 87288)
 Dim EstNOR_Y9 As Decimal = _engine.GetDsDataValue(rid, 87290)
 
 Print(EstNOR_Y7,"Year 7",rid, 12257)
 Print(EstNOR_Y8,"Year 8",rid, 12257)
 Print(EstNOR_Y9,"Year 9",rid, 12257)
 
 Result = (EstNOR_Y7 + EstNOR_Y8 + EstNOR_Y9)
 
 
_engine._productResultCache.Add(__key, result)
Return result
End Function


Public Function GetProductResult_7651(rid As String) As Decimal
Dim __key = "7651" + rid
If (_engine._productResultCache.ContainsKey(__key)) = True Then Return _engine._productResultCache(__key)

 Dim result = 0
 
 
 
 
 
 Dim DateOpened as date = _engine.GetDsDataValue(rid, 9077)
 Dim IsNull As Boolean
 IsNull = IIf(_engine.GetApprovedDsDataValue(rid, 86006), false, true) 
 Dim NOR_Inp_Adj As Decimal = _engine.GetDsDataValue(rid, 86006) 
 Dim NOR_KS3_Cen As Decimal = _engine.GetDsDataValue(rid, 87326)
 Dim NOR_KS3Est As Decimal = GetProductResult_12257(rid) 
 Dim NOR_KS3APT As Decimal = _engine.GetDsDataValue(rid, 86280)
 Dim AcadFilter As Decimal = GetProductResult_7582(rid) 
 Dim FundingBasis as string = GetProductResult_7594(rid) 
 Dim APT_Places As Decimal = _engine.GetDsDataValue(rid, 86369)
 Dim NOR_KS3 As Decimal
 Dim Guaranteed_Numbers As string = _engine.GetDsDataValue(rid, 88442) 
 Dim InpAdj_NOR As Decimal = _engine.GetDsDataValue(rid, 85996)
 Dim RFDC_NOR As Decimal = GetProductResult_10460(rid)  
 
 Print(IsNull, "InpAdj null check", rid, 7651)
 Print(NOR_Inp_Adj, "NOR InpAdj", rid, 7651) 
 Print(FundingBasis,"Funding Basis",rid, 7651)
 Print(DateOpened,"Date Opened",rid, 7651)
 Print(NOR_KS3_Cen,"NOR_KS3Cen",rid, 7651) 
 Print(NOR_KS3Est,"NOR_KS3Est",rid, 7651) 
 Print(NOR_KS3APT,"NOR_KS3APT",rid, 7651) 
 Print(Guaranteed_Numbers, "Guaranteed_Numbers",rid, 7651)
 Print(InpAdj_NOR, "InpAdj_NOR",rid, 7651)
 Print(RFDC_NOR, "RFDC_NOR",rid, 7651)
 
 
 
If currentscenario.periodid = 2017181 And (AcadFilter = 17181) then 
 If FundingBasis = 1 And IsNull = False Then
 NOR_KS3 = NOR_Inp_Adj
 ElseIf FundingBasis = 1 And IsNull = True Then
 NOR_KS3 = NOR_KS3_Cen 
 ElseIf (FundingBasis = 2 And Guaranteed_Numbers = "Yes" And InpAdj_NOR > RFDC_NOR) Then
 NOR_KS3 = NOR_Inp_Adj
 ElseIf FundingBasis = 2 Then
 NOR_KS3 = NOR_KS3Est
 End If
 ElseIf currentscenario.periodid = 2017181 And (AcadFilter = 17182 Or AcadFilter = 17183) Then
 If FundingBasis = 1 Then
 NOR_KS3 = NOR_KS3APT + APT_Places
 ElseIf (FundingBasis = 2 And Guaranteed_Numbers = "Yes" And InpAdj_NOR > RFDC_NOR) Then
 NOR_KS3 = NOR_Inp_Adj
 ElseIf FundingBasis = 2 Then
 NOR_KS3 = NOR_KS3Est
 End If 
 Else Exclude(rid, 7651)
End If
 
 result = NOR_KS3 
 
 
_engine._productResultCache.Add(__key, result)
Return result
End Function


Public Function GetProductResult_12258(rid As String) As Decimal
 Dim result As Decimal = 0
 
Dim __key = "12258" + rid
If (_engine._productResultCache.ContainsKey(__key)) = True Then Return _engine._productResultCache(__key)

 Dim EstNOR_Y10 As Decimal = _engine.GetDsDataValue(rid, 87292)
 Dim EstNOR_Y11 As Decimal = _engine.GetDsDataValue(rid, 87294)
 
 Print(EstNOR_Y10,"Year 10",rid, 12258)
 Print(EstNOR_Y11,"Year 11",rid, 12258)
 
 Result = (EstNOR_Y10 + EstNOR_Y11)
 
 
_engine._productResultCache.Add(__key, result)
Return result
End Function


Public Function GetProductResult_7652(rid As String) As Decimal
Dim __key = "7652" + rid
If (_engine._productResultCache.ContainsKey(__key)) = True Then Return _engine._productResultCache(__key)

 Dim result = 0
 
 
 
 
 
 Dim DateOpened as date = _engine.GetDsDataValue(rid, 9077)
 Dim IsNull As Boolean
 IsNull = IIf(_engine.GetApprovedDsDataValue(rid, 86008), false, true) 
 Dim NOR_Inp_Adj As Decimal = _engine.GetDsDataValue(rid, 86008) 
 Dim NOR_KS4_Cen As Decimal = _engine.GetDsDataValue(rid, 87328)
 Dim NOR_KS4Est As Decimal = GetProductResult_12258(rid)  
 Dim NOR_KS4APT As Decimal = _engine.GetDsDataValue(rid, 86282)
 Dim AcadFilter As Decimal = GetProductResult_7582(rid) 
 Dim FundingBasis as string = GetProductResult_7594(rid) 
 Dim APT_Places As Decimal = _engine.GetDsDataValue(rid, 86371)
 Dim NOR_KS4 As Decimal
 Dim Guaranteed_Numbers As string = _engine.GetDsDataValue(rid, 88442)
 Dim InpAdj_NOR As Decimal = _engine.GetDsDataValue(rid, 85996)
 Dim RFDC_NOR As Decimal = GetProductResult_10460(rid) 
 
 Print(IsNull, "InpAdj null check", rid, 7652)
 Print(NOR_Inp_Adj, "NOR InpAdj", rid, 7652) 
 Print(FundingBasis,"Funding Basis 1617",rid, 7652)
 Print(DateOpened,"Date Opened",rid, 7652)
 Print(FundingBasis,"Abcd",rid, 7652)
 Print(NOR_KS4_Cen,"NOR_KS4 Census",rid, 7652) 
 Print(NOR_KS4Est,"NOR_KS4 Est",rid, 7652) 
 Print(NOR_KS4APT,"NOR_KS4 APT",rid, 7652) 
 Print(Guaranteed_Numbers, "Guaranteed_Numbers",rid, 7652)
 Print(InpAdj_NOR, "InpAdj_NOR",rid, 7652)
 Print(RFDC_NOR, "RFDC_NOR",rid, 7652)


If currentscenario.periodid = 2017181 And (AcadFilter = 17181) then 
 If FundingBasis = 1 And IsNull = False Then
 NOR_KS4 = NOR_Inp_Adj
 ElseIf FundingBasis = 1 And IsNull = True Then
 NOR_KS4 = NOR_KS4_Cen 
 ElseIf (FundingBasis = 2 And Guaranteed_Numbers = "Yes" And InpAdj_NOR > RFDC_NOR) Then
 NOR_KS4 = NOR_Inp_Adj
 ElseIf FundingBasis = 2 Then
 NOR_KS4 = NOR_KS4Est
 End If
 ElseIf currentscenario.periodid = 2017181 And (AcadFilter = 17182 Or AcadFilter = 17183) Then
 If FundingBasis = 1 Then
 NOR_KS4 = NOR_KS4APT + APT_Places
 ElseIf (FundingBasis = 2 And Guaranteed_Numbers = "Yes" And InpAdj_NOR > RFDC_NOR) Then
 NOR_KS4 = NOR_Inp_Adj
 ElseIf FundingBasis = 2 Then
 NOR_KS4 = NOR_KS4Est
 End If 
 Else Exclude(rid, 7652) 
End If

 result = NOR_KS4 
 
 
_engine._productResultCache.Add(__key, result)
Return result
End Function

Public Function GetProductResult_7659(rid As String) As Decimal
Dim __key = "7659" + rid
If (_engine._productResultCache.ContainsKey(__key)) = True Then Return _engine._productResultCache(__key)

 Dim result = 0
 
 
 
 
 
 Dim filter As Decimal = GetProductResult_7582(rid) 
 Dim HNDatasetAPplaces As Decimal = _engine.GetDsDataValue(rid, 87246)
 Dim priNOR As Decimal = GetProductResult_7646(rid) 
 Dim KS3NOR As Decimal = GetProductResult_7651(rid) 
 Dim KS4NOR As Decimal = GetProductResult_7652(rid) 
 Dim unroundedPrisplit As Decimal = HNDatasetAPplaces *Divide(priNOR, (priNOR + KS3NOR + KS4NOR))
 Dim unroundedKS3split As Decimal = HNDatasetAPplaces *Divide(KS3NOR, (priNOR + KS3NOR + KS4NOR))
 Dim unroundedKS4split As Decimal = HNDatasetAPplaces *Divide(KS4NOR, (priNOR + KS3NOR + KS4NOR))
 Dim unadjPrisplit As Decimal = math.round(unroundedPrisplit, 0)
 Dim unadjKS3split As Decimal = math.round(unroundedKS3split, 0)
 Dim unadjKS4split As Decimal = math.round(unroundedKS4split, 0)
 Dim diff As Decimal = unadjPrisplit + unadjKS3split + unadjKS4split - HNDatasetAPplaces 
 
 Print(filter, "filter", rid, 7659)
 Print(HNDatasetAPplaces, "total places", rid, 7659)
 Print(PriNOR, "PriNOR", rid, 7659)
 Print(KS3NOR, "KS3NOR", rid, 7659)
 Print(KS4NOR, "KS4NOR", rid, 7659)
 Print(unroundedPrisplit, "unrounded pri split", rid, 7659)
 Print(unroundedKS3split, "unrounded KS3 split", rid, 7659)
 Print(unroundedKS4split, "unrounded KS4 split", rid, 7659)
 Print(unadjPrisplit, "rounded unadjusted Pri split", rid, 7659)
 Print(unadjKS3split, "rounded unadjusted KS3 split", rid, 7659)
 Print(unadjKS4split, "rounded unadjusted KS4 split", rid, 7659)
 Print(diff, "difference In rounded places total", rid, 7659)
 
 If unadjKS3split = 0 And unadjKS4split = 0 And (diff < 0 Or diff > 0) Then 
 
 result = unadjPrisplit - diff 
 
 else
 
 result = unadjPrisplit
 
 End If 
 
 
 
_engine._productResultCache.Add(__key, result)
Return result
End Function


Public Function GetProductResult_7656(rid As String) As Decimal
Dim __key = "7656" + rid
If (_engine._productResultCache.ContainsKey(__key)) = True Then Return _engine._productResultCache(__key)

 Dim result = 0
 
 
 
 Dim filter As Decimal = GetProductResult_7582(rid) 
 Dim HNDatasetHNplaces As Decimal = _engine.GetDsDataValue(rid, 87244)
 Dim priNOR As Decimal = GetProductResult_7646(rid) 
 Dim KS3NOR As Decimal = GetProductResult_7651(rid) 
 Dim KS4NOR As Decimal = GetProductResult_7652(rid) 
 Dim unroundedPrisplit As Decimal = HNDatasetHNplaces *Divide(priNOR, (priNOR + KS3NOR + KS4NOR))
 Dim unroundedKS3split As Decimal = HNDatasetHNplaces *Divide(KS3NOR, (priNOR + KS3NOR + KS4NOR))
 Dim unroundedKS4split As Decimal = HNDatasetHNplaces *Divide(KS4NOR, (priNOR + KS3NOR + KS4NOR))
 Dim unadjPrisplit As Decimal = math.round(unroundedPrisplit, 0)
 Dim unadjKS3split As Decimal = math.round(unroundedKS3split, 0)
 Dim unadjKS4split As Decimal = math.round(unroundedKS4split, 0)
 Dim diff As Decimal = unadjPrisplit + unadjKS3split + unadjKS4split - HNDatasetHNplaces 
 
 Print(filter, "filter", rid, 7656)
 Print(HNDatasetHNplaces, "total places", rid, 7656)
 Print(PriNOR, "PriNOR", rid, 7656)
 Print(KS3NOR, "KS3NOR", rid, 7656)
 Print(KS4NOR, "KS4NOR", rid, 7656)
 Print(unroundedPrisplit, "unrounded pri split", rid, 7656)
 Print(unroundedKS3split, "unrounded KS3 split", rid, 7656)
 Print(unroundedKS4split, "unrounded KS4 split", rid, 7656)
 Print(unadjPrisplit, "rounded unadjusted Pri split", rid, 7656)
 Print(unadjKS3split, "rounded unadjusted KS3 split", rid, 7656)
 Print(unadjKS4split, "rounded unadjusted KS4 split", rid, 7656)
 Print(diff, "difference In rounded places total", rid, 7656)
 
 If unadjKS3split = 0 And unadjKS4split = 0 And (diff < 0 Or diff > 0) Then 
 
 result = unadjPrisplit - diff 
 
 else
 
 result = unadjPrisplit
 
 End If 
 
 

 
 
_engine._productResultCache.Add(__key, result)
Return result
End Function



Public Function GetProductResult_7593(rid As String) As Decimal
Dim __key = "7593" + rid
If (_engine._productResultCache.ContainsKey(__key)) = True Then Return _engine._productResultCache(__key)

 Dim result As Decimal = 0
 
 Dim F100_AllAcademies As Decimal = GetProductResult_7582(rid) 
 
 If currentscenario.periodid = 2017181 And (F100_AllAcademies = 17181 Or F100_AllAcademies = 17182 Or F100_AllAcademies = 17183) then 
 result = 1
 Else 
 Exclude(rid, 7593) 
 End If 
 
 
_engine._productResultCache.Add(__key, result)
Return result
End Function


Public Function GetProductResult_7638(rid As String) As Decimal
Dim __key = "7638" + rid
If (_engine._productResultCache.ContainsKey(__key)) = True Then Return _engine._productResultCache(__key)

 Dim result = 0
 
 
 Dim HN_UNIT As String = _engine.GetDsDataValue(rid, 86363)
 Dim AcadFilter As Decimal = GetProductResult_7593(rid) 
 
 Print(HN_UNIT, "HN UNIT",rid, 7638)
 Print(AcadFilter, "AcadFilter", rid, 7638)
 
If HN_UNIT isnot nothing then 
 If AcadFilter = 1 then 
 If HN_UNIT.Contains("Yes") Then 
 result = 1
 Else
 Result = 0
 End If
 Else Exclude(rid, 7638) 
 End If
End If 
 
 
_engine._productResultCache.Add(__key, result)
Return result
End Function


Public Function GetProductResult_7667(rid As String) As Decimal
Dim __key = "7667" + rid
If (_engine._productResultCache.ContainsKey(__key)) = True Then Return _engine._productResultCache(__key)

 Dim result = 0
 
 

 Dim AcadFilter As Decimal = GetProductResult_7582(rid) 
 Dim TotalPlacesAPT As Decimal = _engine.GetDsDataValue(rid, 86365)
 Dim IsNull As Boolean
 IsNull = IIf(_engine.GetApprovedDsDataValue(rid, 86365), false, true) 
 Dim NOR_Inp_Adj As Decimal = _engine.GetDsDataValue(rid, 85998) 
 Dim Pre16HNData As Decimal = _engine.GetDsDataValue(rid, 87244)
 Dim Pre16APData As Decimal = _engine.GetDsDataValue(rid, 87246)
 Dim TotalPlacesHNData As Decimal = Pre16HNData + Pre16APData
 Dim NOR_Pri As Decimal = GetProductResult_7646(rid) 
 Dim RU_NOR As Decimal = GetProductResult_7645(rid) 
 Dim APT_HN_Pri As Decimal = GetProductResult_7653(rid)  
 Dim HND_AP_Primary As Decimal= GetProductResult_7659(rid) 
 Dim HND_HNP_Pri As Decimal = GetProductResult_7656(rid)  
 Dim HND_HN_Pri As Decimal = HND_HNP_Pri + HND_AP_Primary 
 Dim HN_Unit As Decimal = GetProductResult_7638(rid)  
 Dim HN_to_Deduct As Decimal
 Dim FundingBasis As Decimal = GetProductResult_7594(rid) 
 



If currentscenario.periodid = 2017181 And (AcadFilter = 17181 Or AcadFilter = 17182 Or AcadFilter = 17183) then
 If (IsNull = True Or TotalPlacesAPT = 0) And TotalPlacesHNData > 0 Then
 HN_to_deduct = 0
 Else If TotalPlacesAPT > TotalPlacesHNData Then
 HN_to_deduct = HND_HN_Pri 
 Else 
 HN_to_deduct = APT_HN_Pri
 End If 
Else Exclude(rid, 7667)
End If 
 
 
 
 
 
 Print(TotalPlacesAPT, "Places from APT",rid, 7667)
 Print(TotalPlacesHNData, "Places from HN Data",rid, 7667)
 Print(NOR_Pri, "NOR Pri", rid, 7667)
 Print(RU_NOR, "Recep UP", rid, 7667)
 Print(APT_HN_Pri, "APT Pri", rid, 7667)
 Print(HND_AP_Primary, "HNP Pri", rid, 7667)
 Print(HND_HNP_Pri, "HND APPri", rid, 7667)
 Print(HND_HN_Pri, "HND Pri", rid, 7667)
 Print(HN_Unit,"HN unit",rid, 7667)
 Print(HN_to_deduct, "HN todeduct", rid, 7667)
 
 
 
 
 If currentscenario.periodid = 2017181 And (AcadFilter = 17182 Or AcadFilter = 17183) And FundingBasis = 1 Then 
 Result = NOR_Pri + RU_NOR
 Else 
 Result = NOR_Pri + RU_NOR - HN_to_deduct 
 End If 
 
 
 
 
_engine._productResultCache.Add(__key, result)
Return result
End Function


Public Function GetProductResult_8152(rid As String) As Decimal
Dim __key = "8152" + rid
If (_engine._productResultCache.ContainsKey(__key)) = True Then Return _engine._productResultCache(__key)

 Dim result = 0
 
 Dim AcademyFilter As Decimal = GetProductResult_7582(rid) 
 Dim FundingBasis As String = _engine.GetDsDataValue(rid, 11187, "2017181")
 
 Print(AcademyFilter,"AcademyFilter",rid, 8152)
 Print(FundingBasis,"FundingBasis",rid, 8152)
 
 If FundingBasis = "Place" Then 
 
 exclude(rid, 8152) 
 
 Else
 
 If AcademyFilter = 17181 Or AcademyFilter = 17182 Or AcademyFilter = 17183 then
 
 Dim Providers As String = _engine.GetDsDataValue(rid, 9070)
 Dim PriRate As Decimal = LAtoProv(_engine.GetLaDataValue(rid, 86416, Nothing))
 
 result = PriRate 
 
 Else Exclude(rid, 8152)
 
 End if
 
 Print(AcademyFilter, "Filter", rid, 8152)
 
 End If 
 
 
result = System.Math.Round(result, 6, MidpointRounding.AwayFromZero)
_engine._productResultCache.Add(__key, result)
Return result
 
End Function


 Function GetProductResult_8153(rid As String) As Decimal
Dim __key = "8153" + rid
If (_engine._productResultCache.ContainsKey(__key)) = True Then Return _engine._productResultCache(__key)

 Dim result = 0
 
 Dim AcademyFilter As Decimal = GetProductResult_7582(rid) 
 Dim FundingBasis As String = _engine.GetDsDataValue(rid, 11187, "2017181")
 
 
 
 Print(AcademyFilter,"AcademyFilter",rid, 8153)
 Print(FundingBasis,"FundingBasis",rid, 8153)
 
 If FundingBasis = "Place" Then
 
 exclude(rid, 8153) 
 
 Else
 
 
 
 
 
 
 
 
 
 
 
 End if
 
 print(FundingBasis, "FundingBasisType",rid, 8153)
 print(AcademyFilter, "AcademyFilter", rid, 8153)
 
 If AcademyFilter = 17181 Or (AcademyFilter = 17182 And FundingBasis = "Estimate") Or (AcademyFilter = 17183 And FundingBasis = "Estimate") THEN
 
 
 
 Dim Primary_NOR As Decimal = GetProductResult_7667(rid)  
 Dim P004_PriRate As Decimal = GetProductResult_8152(rid)  
 Dim PriBESubtotal As Decimal = Primary_NOR * P004_PriRate
 
 Print(Primary_NOR,"Primary Pupils",rid, 8153)
 Print(P004_PriRate,"P004_PriRate",rid, 8153) 
 
 result = PriBESubtotal
 
 else
 
 
 
 If (AcademyFilter = 17182 And FundingBasis = "Census") Or (AcademyFilter = 17183 And FundingBasis = "Census") then
 
 Dim PriBESubtotalAPT As Decimal = _engine.GetDsDataValue(rid, 86092) 

 Print(PriBESubtotalAPT,"PriBESubtotalAPT",rid, 8153)

 Result = PriBESubtotalAPT
 
 Else 
 
 exclude(rid, 8153)
 
 End If
 
 
 End if
 
 
 
 
result = System.Math.Round(result, 2, MidpointRounding.AwayFromZero)
_engine._productResultCache.Add(__key, result)
Return result
 
End Function



Public Function GetProductResult_7623(rid As String) As Decimal
Dim __key = "7623" + rid
If (_engine._productResultCache.ContainsKey(__key)) = True Then Return _engine._productResultCache(__key)

 Dim result = 0
 Dim AcademyFilter As Decimal = GetProductResult_7582(rid) 
 
 
 
 
 
 Dim FundingBasis As String = _engine.GetDsDataValue(rid, 11187, "2017181")
 Dim DateOpened As Date = _engine.GetDsDataValue(rid, 9077)
 Dim ConvertDate As Date = _engine.GetDsDataValue(rid, 73189)
 Dim DateUsed As Date
 Dim Days_Open As Decimal = 0
 
 If ConvertDate >= DateOpened Then
 DateUsed = ConvertDate
 Else
 DateUsed = DateOpened
 End If 
 
 
 Print(DateOpened, "Date Opened", rid, 7623)
 Print(ConvertDate, "Convert Date",rid, 7623)
 Print(AcademyFilter,"AcademyFilter",rid, 7623)
 Print(FundingBasis,"FundingBasis",rid, 7623)
 
 
 
 If AcademyFilter > 0 then
 
 Days_Open = DateDiff("d", DateUsed, "1 September 2018")
 
 
 Print(Days_Open, "Number of Days Open to end 1718", rid, 7623) 
 Print(AcademyFilter, "Filter", rid, 7623)
 
 if Days_Open > 365 then

 result = 365

 else

 result = Days_Open
 
 End if
 
 
 else
 exclude(rid, 7623)
 End If 
 
 
_engine._productResultCache.Add(__key, result)
Return result
End Function

 Public Function GetProductResult_8151(rid As String) As Decimal
Dim __key = "8151" + rid
If (_engine._productResultCache.ContainsKey(__key)) = True Then Return _engine._productResultCache(__key)

 Dim result = 0
 
 Dim AcademyFilter As Decimal = GetProductResult_7582(rid) 
 
 
 
 Dim FundingBasis As String = _engine.GetDsDataValue(rid, 11187, "2017181")
 Dim DaysOpen As Decimal = GetProductResult_7623(rid) 
 
 
 
 Print(AcademyFilter,"AcademyFilter",rid, 8151)
 Print(FundingBasis,"FundingBasis",rid, 8151)
 
 If FundingBasis = "Place" Then
 
 exclude(rid, 8151)
 
 Else
 
 If AcademyFilter = 17181 Or AcademyFilter = 17182 Or AcademyFilter = 17183 then
 
 Result = DaysOpen
 
 
 else
 
 exclude(rid, 8151)
 
 End If 
 
 End If 
 
 
result = System.Math.Round(result, 6, MidpointRounding.AwayFromZero)
_engine._productResultCache.Add(__key, result)
Return result
 
End Function

Public Function GetProductResult_8156(rid As String) As Decimal
Dim __key = "8156" + rid
If (_engine._productResultCache.ContainsKey(__key)) = True Then Return _engine._productResultCache(__key)

 Dim result = 0
 
 Dim AcademyFilter As Decimal = GetProductResult_7582(rid) 
 Dim FundingBasis As String = _engine.GetDsDataValue(rid, 11187, "2017181")
 
 Print(AcademyFilter,"AcademyFilter",rid, 8156)
 Print(FundingBasis,"FundingBasis",rid, 8156)
 
 If FundingBasis = "Place" Then
 
 exclude(rid, 8156) 
 
 Else
 
 If AcademyFilter = 17181 Or AcademyFilter = 17182 Or AcademyFilter = 17183 then
 
 Dim P005_PriBESubtotal As Decimal = GetProductResult_8153(rid)  
 Dim Days_Open As Decimal = GetProductResult_8151(rid) 
 Dim Year_Days As Decimal = 365 
 
 Print(P005_PriBESubtotal,"P145 P005_PriBESubtotal",rid, 8156)
 Print(Days_Open,"Days Open",rid, 8156)
 Print(Year_Days,"Year Days",rid, 8156)
 
 Result = (P005_PriBESubtotal) *Divide(Days_Open, Year_Days)
 
 Else
 
 Exclude(rid, 8156)
 
 End If
 
 End If
 
 
result = System.Math.Round(result, 6, MidpointRounding.AwayFromZero)
_engine._productResultCache.Add(__key, result)
Return result
 
End Function

 Public Function GetProductResult_8158(rid As String) As Decimal
Dim __key = "8158" + rid
If (_engine._productResultCache.ContainsKey(__key)) = True Then Return _engine._productResultCache(__key)

 Dim result = 0
 
 Dim AcademyFilter As Decimal = GetProductResult_7582(rid) 
 Dim FundingBasis As String = _engine.GetDsDataValue(rid, 11187, "2017181")
 
 Print(AcademyFilter,"AcademyFilter",rid, 8158)
 Print(FundingBasis,"FundingBasis",rid, 8158)
 
 If FundingBasis = "Place" Then
 
 exclude(rid, 8158)
 
 Else 

 If AcademyFilter = 17181 Or AcademyFilter = 17182 Or AcademyFilter = 17183 then
 
 DIM P009_KS3Rate = LAtoProv(_engine.GetLaDataValue(rid, 86426, Nothing))
 
 result = P009_KS3Rate
 
 Else Exclude(rid, 8158)
 
 End if
 
 End If
 
 
result = System.Math.Round(result, 6, MidpointRounding.AwayFromZero)
_engine._productResultCache.Add(__key, result)
Return result
 
End Function

Public Function GetProductResult_7654(rid As String) As Decimal
Dim __key = "7654" + rid
If (_engine._productResultCache.ContainsKey(__key)) = True Then Return _engine._productResultCache(__key)

 Dim result = 0
 
 
 
 
 Dim HN_UNIT as String = _engine.GetDsDataValue(rid, 86363)
 Dim HN_KS3 As Decimal = _engine.GetDsDataValue(rid, 86369)
 Dim AcadFilter As Decimal = GetProductResult_7582(rid) 


If currentscenario.periodid = 2017181 And (AcadFilter = 17181 Or AcadFilter = 17182 Or AcadFilter = 17183) then 
 result = HN_KS3 
Else Exclude(rid, 7654) 
End if

 
_engine._productResultCache.Add(__key, Result)
Return Result
End Function




Public Function GetProductResult_7660(rid As String) As Decimal
Dim __key = "7660" + rid
If (_engine._productResultCache.ContainsKey(__key)) = True Then Return _engine._productResultCache(__key)

 Dim result = 0
 
 
 
 
 
 Dim filter As Decimal = GetProductResult_7582(rid) 
 Dim HNDatasetAPplaces As Decimal = _engine.GetDsDataValue(rid, 87246)
 Dim priNOR As Decimal = GetProductResult_7646(rid) 
 Dim KS3NOR As Decimal = GetProductResult_7651(rid) 
 Dim KS4NOR As Decimal = GetProductResult_7652(rid) 
 Dim unroundedPrisplit As Decimal = HNDatasetAPplaces *Divide(priNOR, (priNOR + KS3NOR + KS4NOR))
 Dim unroundedKS3split As Decimal = HNDatasetAPplaces *Divide(KS3NOR, (priNOR + KS3NOR + KS4NOR))
 Dim unroundedKS4split As Decimal = HNDatasetAPplaces *Divide(KS4NOR, (priNOR + KS3NOR + KS4NOR))
 Dim unadjPrisplit As Decimal = math.round(unroundedPrisplit, 0)
 Dim unadjKS3split As Decimal = math.round(unroundedKS3split, 0)
 Dim unadjKS4split As Decimal = math.round(unroundedKS4split, 0)
 Dim diff As Decimal = unadjPrisplit + unadjKS3split + unadjKS4split - HNDatasetAPplaces 
 
 Print(filter, "filter", rid, 7660)
 Print(HNDatasetAPplaces, "total places", rid, 7660)
 Print(PriNOR, "PriNOR", rid, 7660)
 Print(KS3NOR, "KS3NOR", rid, 7660)
 Print(KS4NOR, "KS4NOR", rid, 7660)
 Print(unroundedPrisplit, "unrounded pri split", rid, 7660)
 Print(unroundedKS3split, "unrounded KS3 split", rid, 7660)
 Print(unroundedKS4split, "unrounded KS4 split", rid, 7660)
 Print(unadjPrisplit, "rounded unadjusted Pri split", rid, 7660)
 Print(unadjKS3split, "rounded unadjusted KS3 split", rid, 7660)
 Print(unadjKS4split, "rounded unadjusted KS4 split", rid, 7660)
 Print(diff, "difference In rounded places total", rid, 7660)
 
 If unadjKS3split > 0 And (diff < 0 Or diff > 0) And unadjKS4split = 0 Then 
 
 result = unadjKS3split - diff 
 
 else
 
 result = unadjKS3split
 
 End If 
 
_engine._productResultCache.Add(__key, result)
Return result
End Function


Public Function GetProductResult_7657(rid As String) As Decimal
Dim __key = "7657" + rid
If (_engine._productResultCache.ContainsKey(__key)) = True Then Return _engine._productResultCache(__key)

 Dim result = 0
 
 
 
 
 
 Dim filter As Decimal = GetProductResult_7582(rid) 
 Dim HNDatasetHNplaces As Decimal = _engine.GetDsDataValue(rid, 87244)
 Dim priNOR As Decimal = GetProductResult_7646(rid) 
 Dim KS3NOR As Decimal = GetProductResult_7651(rid) 
 Dim KS4NOR As Decimal = GetProductResult_7652(rid) 
 Dim unroundedPrisplit As Decimal = HNDatasetHNplaces *Divide(priNOR, (priNOR + KS3NOR + KS4NOR))
 Dim unroundedKS3split As Decimal = HNDatasetHNplaces *Divide(KS3NOR, (priNOR + KS3NOR + KS4NOR))
 Dim unroundedKS4split As Decimal = HNDatasetHNplaces *Divide(KS4NOR, (priNOR + KS3NOR + KS4NOR))
 Dim unadjPrisplit As Decimal = math.round(unroundedPrisplit, 0)
 Dim unadjKS3split As Decimal = math.round(unroundedKS3split, 0)
 Dim unadjKS4split As Decimal = math.round(unroundedKS4split, 0)
 Dim diff As Decimal = unadjPrisplit + unadjKS3split + unadjKS4split - HNDatasetHNplaces 
 
 Print(filter, "filter", rid, 7657)
 Print(HNDatasetHNplaces, "total places", rid, 7657)
 Print(PriNOR, "PriNOR", rid, 7657)
 Print(KS3NOR, "KS3NOR", rid, 7657)
 Print(KS4NOR, "KS4NOR", rid, 7657)
 Print(unroundedPrisplit, "unrounded pri split", rid, 7657)
 Print(unroundedKS3split, "unrounded KS3 split", rid, 7657)
 Print(unroundedKS4split, "unrounded KS4 split", rid, 7657)
 Print(unadjPrisplit, "rounded unadjusted Pri split", rid, 7657)
 Print(unadjKS3split, "rounded unadjusted KS3 split", rid, 7657)
 Print(unadjKS4split, "rounded unadjusted KS4 split", rid, 7657)
 Print(diff, "difference In rounded places total", rid, 7657)
 
 If unadjKS3split > 0 And (diff < 0 Or diff > 0) And unadjKS4split = 0 Then 
 
 result = unadjKS3split - diff 
 
 else
 
 result = unadjKS3split
 
 End If 
 
 
_engine._productResultCache.Add(__key, result)
Return result
End Function



Public Function GetProductResult_7669(rid As String) As Decimal
Dim __key = "7669" + rid
If (_engine._productResultCache.ContainsKey(__key)) = True Then Return _engine._productResultCache(__key)

 Dim result = 0
 
 
 
 
 
 Dim AcadFilter As Decimal = GetProductResult_7582(rid) 
 Dim TotalPlacesAPT As Decimal = _engine.GetDsDataValue(rid, 86365)
 Dim IsNull As Boolean
 IsNull = IIf(_engine.GetApprovedDsDataValue(rid, 86365), false, true) 
 Dim NOR_Inp_Adj As Decimal = _engine.GetDsDataValue(rid, 86006)
 Dim Pre16HNData As Decimal = _engine.GetDsDataValue(rid, 87244)
 Dim Pre16APData As Decimal = _engine.GetDsDataValue(rid, 87246)
 Dim TotalPlacesHNData As Decimal = Pre16HNData + Pre16APData
 Dim NOR_KS3 As Decimal = GetProductResult_7651(rid) 
 Dim APT_HN_KS3 As Decimal = GetProductResult_7654(rid)  
 Dim HND_AP_KS3 As Decimal= GetProductResult_7660(rid) 
 Dim HND_HNP_KS3 As Decimal = GetProductResult_7657(rid)  
 Dim HND_HN_KS3 As Decimal = HND_AP_KS3 + HND_HNP_KS3 
 Dim HN_Unit As Decimal = GetProductResult_7638(rid)  
 Dim Fundingbasis As Decimal = GetProductResult_7594(rid) 
 Dim HN_to_Deduct As Decimal
 



 
If currentscenario.periodid = 2017181 And (AcadFilter = 17181 Or AcadFilter = 171872 Or AcadFilter = 17183) then
 If (IsNull = True Or TotalPlacesAPT = 0) And TotalPlacesHNData > 0 Then
 HN_to_deduct = 0
 Else If TotalPlacesAPT > TotalPlacesHNData Then
 HN_to_deduct = HND_HN_KS3 
 Else 
 HN_to_deduct = APT_HN_KS3
 End If 
Else Exclude(rid, 7669)
End If 
 
 
 Print(Fundingbasis, "funding basis", rid, 7669)
 Print(NOR_KS3, "NOR KS3", rid, 7669)
 Print(APT_HN_KS3, "APTKS3", rid, 7669)
 Print(HND_AP_KS3, "HNP KS3", rid, 7669)
 Print(HND_HNP_KS3, "HND APKS3", rid, 7669)
 Print(HND_HN_KS3, "HND KS3", rid, 7669)
 Print(HN_Unit,"HN unit",rid, 7669)
 Print(HN_to_deduct, "HN todeduct", rid, 7669)
 
 If currentscenario.periodid = 2017181 And (AcadFilter = 17182 Or AcadFilter = 17183) And Fundingbasis = 1 Then 
 Result = NOR_KS3
 Else 
 Result = NOR_KS3 - HN_to_deduct 
 End If
 
 
_engine._productResultCache.Add(__key, Result)
Return Result
End Function



 Function GetProductResult_8159(rid As String) As Decimal
Dim __key = "8159" + rid
If (_engine._productResultCache.ContainsKey(__key)) = True Then Return _engine._productResultCache(__key)

 Dim result = 0
 
 Dim AcademyFilter As Decimal = GetProductResult_7582(rid) 
 Dim FundingBasis As String = _engine.GetDsDataValue(rid, 11187, "2017181")
 
 Print(AcademyFilter,"AcademyFilter",rid, 8159)
 Print(FundingBasis,"FundingBasis",rid, 8159)
 
 If FundingBasis = "Place" Then
 
 exclude(rid, 8159) 
 
 Else
 
 
 
 
 
 
 
 
 
 
 
 End if
 
 print(FundingBasis, "FundingBasisType",rid, 8159)
 print(AcademyFilter, "AcademyFilter", rid, 8159)
 
 If AcademyFilter = 17181 Or (AcademyFilter = 17182 And FundingBasis = "Estimate") Or (AcademyFilter = 17183 And FundingBasis = "Estimate") THEN
 
 Dim P009_KS3Rate As Decimal = GetProductResult_8158(rid) 
 Dim KS3_NOR As Decimal = GetProductResult_7669(rid)  
 Dim KS3BESubtotal As Decimal = KS3_NOR * P009_KS3Rate
 
 result = KS3BESubtotal
 
 Print(KS3_NOR,"KS3 Pupils",rid, 8159)
 Print(P009_KS3Rate,"KS3 Basic Entitlement Rate",rid, 8159) 
 
 else
 
 If (AcademyFilter = 17182 And FundingBasis = "Census") Or (AcademyFilter = 17183 And FundingBasis = "Census") then
 
 Dim KS3BESubtotalAPT As Decimal = _engine.GetDsDataValue(rid, 86094) 

 Print(KS3BESubtotalAPT,"KS3BESubtotalAPT",rid, 8159)

 Result = KS3BESubtotalAPT
 
 Else 
 
 exclude(rid, 8159)
 
 End If 
 End If
 
 
 
 
result = System.Math.Round(result, 2, MidpointRounding.AwayFromZero)
_engine._productResultCache.Add(__key, result)
Return result
 
End Function


Public Function GetProductResult_8162(rid As String) As Decimal
Dim __key = "8162" + rid
If (_engine._productResultCache.ContainsKey(__key)) = True Then Return _engine._productResultCache(__key)

 Dim result = 0
 
 Dim AcademyFilter As Decimal = GetProductResult_7582(rid) 
 Dim FundingBasis As String = _engine.GetDsDataValue(rid, 11187, "2017181")
 
 Print(AcademyFilter,"AcademyFilter",rid, 8162)
 Print(FundingBasis,"FundingBasis",rid, 8162)
 
 If FundingBasis = "Place" Then 
 
 exclude(rid, 8162) 
 
 Else 
 
 If AcademyFilter = 17181 Or AcademyFilter = 17182 Or AcademyFilter = 17183 then
 
 Dim P010_KS3_BESubtotal As Decimal = GetProductResult_8159(rid) 
 Dim Days_Open As Decimal = GetProductResult_8151(rid) 
 Dim Year_Days As Decimal = 365 
 
 Print(P010_KS3_BESubtotal,"P145 P010_KS3_BESubtotal",rid, 8162)
 Print(Days_Open,"Days Open",rid, 8162)
 Print(Year_Days,"Year Days",rid, 8162)
 
 Result = (P010_KS3_BESubtotal) *Divide(Days_Open, Year_Days)
 
 Else
 
 Exclude(rid, 8162) 
 
 End If 
 
 End If
 
 
result = System.Math.Round(result, 6, MidpointRounding.AwayFromZero)
_engine._productResultCache.Add(__key, result)
Return result
 
End Function

Public Function GetProductResult_7655(rid As String) As Decimal
Dim __key = "7655" + rid
If (_engine._productResultCache.ContainsKey(__key)) = True Then Return _engine._productResultCache(__key)

 Dim result = 0
 
 
 
 
 Dim HN_UNIT as String = _engine.GetDsDataValue(rid, 86363)
 Dim HN_KS4 As Decimal = _engine.GetDsDataValue(rid, 86371)
 Dim AcadFilter = GetProductResult_7582(rid) 

Print(HN_UNIT, "HN Unit",rid, 7655)
Print(HN_KS4, "HN KS4 Places", rid, 7655)

If currentscenario.periodid = 2017181 And (AcadFilter = 17181 Or AcadFilter = 17182 Or AcadFilter = 17183) then 
 result = HN_KS4
Else Exclude(rid, 7655) 
End if
 
_engine._productResultCache.Add(__key, Result)
Return Result
End Function


Public Function GetProductResult_7661(rid As String) As Decimal
Dim __key = "7661" + rid
If (_engine._productResultCache.ContainsKey(__key)) = True Then Return _engine._productResultCache(__key)

 Dim result = 0
 
 
 
 
 
 Dim filter As Decimal = GetProductResult_7582(rid) 
 Dim HNDatasetAPplaces As Decimal = _engine.GetDsDataValue(rid, 87246)
 Dim priNOR As Decimal = GetProductResult_7646(rid) 
 Dim KS3NOR As Decimal = GetProductResult_7651(rid) 
 Dim KS4NOR As Decimal = GetProductResult_7652(rid) 
 Dim unroundedPrisplit As Decimal = HNDatasetAPplaces *Divide(priNOR, (priNOR + KS3NOR + KS4NOR))
 Dim unroundedKS3split As Decimal = HNDatasetAPplaces *Divide(KS3NOR, (priNOR + KS3NOR + KS4NOR))
 Dim unroundedKS4split As Decimal = HNDatasetAPplaces *Divide(KS4NOR, (priNOR + KS3NOR + KS4NOR))
 Dim unadjPrisplit As Decimal = math.round(unroundedPrisplit, 0)
 Dim unadjKS3split As Decimal = math.round(unroundedKS3split, 0)
 Dim unadjKS4split As Decimal = math.round(unroundedKS4split, 0)
 Dim diff As Decimal = unadjPrisplit + unadjKS3split + unadjKS4split - HNDatasetAPplaces 
 
 Print(filter, "filter", rid, 7661)
 Print(HNDatasetAPplaces, "total places", rid, 7661)
 Print(PriNOR, "PriNOR", rid, 7661)
 Print(KS3NOR, "KS3NOR", rid, 7661)
 Print(KS4NOR, "KS4NOR", rid, 7661)
 Print(unroundedPrisplit, "unrounded pri split", rid, 7661)
 Print(unroundedKS3split, "unrounded KS3 split", rid, 7661)
 Print(unroundedKS4split, "unrounded KS4 split", rid, 7661)
 Print(unadjPrisplit, "rounded unadjusted Pri split", rid, 7661)
 Print(unadjKS3split, "rounded unadjusted KS3 split", rid, 7661)
 Print(unadjKS4split, "rounded unadjusted KS4 split", rid, 7661)
 Print(diff, "difference In rounded places total", rid, 7661)
 
 
 If unadjKS4split > 0 And (diff < 0 Or diff > 0) Then 
 
 result = unadjKS4split - diff 
 
 else
 
 result = unadjKS4split
 
 End If 
 

 
_engine._productResultCache.Add(__key, result)
Return result
End Function


Public Function GetProductResult_7658(rid As String) As Decimal
Dim __key = "7658" + rid
If (_engine._productResultCache.ContainsKey(__key)) = True Then Return _engine._productResultCache(__key)

 Dim result = 0
 
 
 
 
 
 Dim filter As Decimal = GetProductResult_7582(rid) 
 Dim HNDatasetHNplaces As Decimal = _engine.GetDsDataValue(rid, 87244)
 Dim priNOR As Decimal = GetProductResult_7646(rid) 
 Dim KS3NOR As Decimal = GetProductResult_7651(rid) 
 Dim KS4NOR As Decimal = GetProductResult_7652(rid) 
 Dim unroundedPrisplit As Decimal = HNDatasetHNplaces *Divide(priNOR, (priNOR + KS3NOR + KS4NOR))
 Dim unroundedKS3split As Decimal = HNDatasetHNplaces *Divide(KS3NOR, (priNOR + KS3NOR + KS4NOR))
 Dim unroundedKS4split As Decimal = HNDatasetHNplaces *Divide(KS4NOR, (priNOR + KS3NOR + KS4NOR))
 Dim unadjPrisplit As Decimal = math.round(unroundedPrisplit, 0)
 Dim unadjKS3split As Decimal = math.round(unroundedKS3split, 0)
 Dim unadjKS4split As Decimal = math.round(unroundedKS4split, 0)
 Dim diff As Decimal = unadjPrisplit + unadjKS3split + unadjKS4split - HNDatasetHNplaces 
 
 Print(filter, "filter", rid, 7658)
 Print(HNDatasetHNplaces, "total places", rid, 7658)
 Print(PriNOR, "PriNOR", rid, 7658)
 Print(KS3NOR, "KS3NOR", rid, 7658)
 Print(KS4NOR, "KS4NOR", rid, 7658)
 Print(unroundedPrisplit, "unrounded pri split", rid, 7658)
 Print(unroundedKS3split, "unrounded KS3 split", rid, 7658)
 Print(unroundedKS4split, "unrounded KS4 split", rid, 7658)
 Print(unadjPrisplit, "rounded unadjusted Pri split", rid, 7658)
 Print(unadjKS3split, "rounded unadjusted KS3 split", rid, 7658)
 Print(unadjKS4split, "rounded unadjusted KS4 split", rid, 7658)
 Print(diff, "difference In rounded places total", rid, 7658)
 
 
 If unadjKS4split > 0 And (diff < 0 Or diff > 0) Then 
 
 result = unadjKS4split - diff 
 
 else
 
 result = unadjKS4split
 
 End If 
 
 
_engine._productResultCache.Add(__key, result)
Return result
End Function


Public Function GetProductResult_7671(rid As String) As Decimal
Dim __key = "7671" + rid
If (_engine._productResultCache.ContainsKey(__key)) = True Then Return _engine._productResultCache(__key)

 Dim result = 0
 
 
 
 
 
 Dim AcadFilter As Decimal = GetProductResult_7582(rid) 
 Dim TotalPlacesAPT As Decimal = _engine.GetDsDataValue(rid, 86365)
 Dim IsNull As Boolean
 IsNull = IIf(_engine.GetApprovedDsDataValue(rid, 86365), false, true) 
 Dim NOR_Inp_Adj As Decimal = _engine.GetDsDataValue(rid, 86008) 
 Dim Pre16HNData As Decimal = _engine.GetDsDataValue(rid, 87244)
 Dim Pre16APData As Decimal = _engine.GetDsDataValue(rid, 87246)
 Dim TotalPlacesHNData As Decimal = Pre16HNData + Pre16APData
 Dim NOR_KS4 As Decimal = GetProductResult_7652(rid) 
 Dim APT_HN_KS4 As Decimal = GetProductResult_7655(rid)  
 Dim HND_AP_KS4 As Decimal= GetProductResult_7661(rid) 
 Dim HND_HNP_KS4 As Decimal = GetProductResult_7658(rid)  
 Dim HND_HN_KS4 As Decimal = GetProductResult_7658(rid)  + GetProductResult_7661(rid)  
 Dim HN_Unit As Decimal = GetProductResult_7638(rid)  
 Dim Fundingbasis As Decimal = GetProductResult_7594(rid) 
 Dim HN_to_Deduct As Decimal
 



If currentscenario.periodid = 2017181 And (AcadFilter = 17181 Or AcadFilter = 17182 Or AcadFilter = 17183) then
 If (IsNull = True Or TotalPlacesAPT = 0) And TotalPlacesHNData > 0 Then
 HN_to_deduct = 0
 Else If TotalPlacesAPT > TotalPlacesHNData Then
 HN_to_deduct = HND_HN_KS4 
 Else 
 HN_to_deduct = APT_HN_KS4
 End If 
Else Exclude(rid, 7671)
End If 
 
 Print(Fundingbasis, "funding basis", rid, 7671)
 Print(NOR_KS4, "NOR KS4", rid, 7671)
 Print(APT_HN_KS4, "APTKS4", rid, 7671)
 Print(HND_AP_KS4, "HNP KS4", rid, 7671)
 Print(HND_HNP_KS4, "HND APKS4", rid, 7671)
 Print(HND_HN_KS4, "HND KS4", rid, 7671)
 Print(HN_Unit,"HN unit",rid, 7671)
 Print(HN_to_deduct, "HN todeduct", rid, 7671)
 
 If currentscenario.periodid = 2017181 And (AcadFilter = 17182 Or AcadFilter = 17183) And Fundingbasis = 1 Then 
 Result = NOR_KS4
 Else 
 Result = NOR_KS4 - HN_to_deduct 
 End If
 
 
_engine._productResultCache.Add(__key, result)
Return result
End Function



 Public Function GetProductResult_8163(rid As String) As Decimal
Dim __key = "8163" + rid
If (_engine._productResultCache.ContainsKey(__key)) = True Then Return _engine._productResultCache(__key)

 Dim result = 0
 
 Dim AcademyFilter As Decimal = GetProductResult_7582(rid) 
 Dim FundingBasis As String = _engine.GetDsDataValue(rid, 11187, "2017181")
 
 Print(Academyfilter,"AcademyFilter",rid, 8163)
 Print(FundingBasis,"FundingBasis",rid, 8163)
 
 If FundingBasis = "Place" Then
 
 exclude(rid, 8163) 
 
 Else

 If AcademyFilter = 17181 Or AcademyFilter = 17182 Or AcademyFilter = 17183 then
 
 Dim providers As String = _engine.GetDsDataValue(rid, 9070)
 Dim P014_KS4Rate = LAtoProv(_engine.GetLaDataValue(rid, 86436, Nothing))
 
 Result = P014_KS4Rate
 
 Else Exclude(rid, 8163)
 
 End If 
 
 End If
 
 
result = System.Math.Round(result, 6, MidpointRounding.AwayFromZero)
_engine._productResultCache.Add(__key, result)
Return result
 
End Function


 Function GetProductResult_8164(rid As String) As Decimal
Dim __key = "8164" + rid
If (_engine._productResultCache.ContainsKey(__key)) = True Then Return _engine._productResultCache(__key)

 Dim result = 0
 
 Dim AcademyFilter As Decimal = GetProductResult_7582(rid) 
 Dim FundingBasis As String = _engine.GetDsDataValue(rid, 11187, "2017181")
 
 Print(AcademyFilter,"AcademyFilter",rid, 8164)
 Print(FundingBasis,"FundingBasis",rid, 8164)
 
 If FundingBasis = "Place" Then
 
 exclude(rid, 8164)
 
 Else
 
 
 
 
 
 
 
 
 
 
 
 End if
 
 print(FundingBasis, "FundingBasisType",rid, 8164)
 print(AcademyFilter, "AcademyFilter", rid, 8164)
 
 If AcademyFilter = 17181 Or (AcademyFilter = 17182 And FundingBasis = "Estimate") Or (AcademyFilter = 17183 And FundingBasis = "Estimate") THEN
 
 Dim KS4_NOR As Decimal = GetProductResult_7671(rid)  
 Dim P014_KS4Rate As Decimal = GetProductResult_8163(rid)  
 Dim KS4BESubtotal As Decimal = KS4_NOR * P014_KS4Rate
 
 Print(KS4_NOR,"KS4 Pupils",rid, 8164)
 Print(P014_KS4Rate,"KS4 Basic Entitlement Rate",rid, 8164)
 
 result = KS4BESubtotal
 
 else
 
 
 If (AcademyFilter = 17182 And FundingBasis = "Census") Or (AcademyFilter = 17183 And FundingBasis = "Census") then
 
 Dim KS4BESubtotalAPT As Decimal = _engine.GetDsDataValue(rid, 86096) 

 Print(KS4BESubtotalAPT,"KS4BESubtotalAPT",rid, 8164)

 Result = KS4BESubtotalAPT
 
 Else
 
 exclude(rid, 8164)
 
 End If
 
 End if
 
 
 
 
result = System.Math.Round(result, 2, MidpointRounding.AwayFromZero)
_engine._productResultCache.Add(__key, result)
Return result
 
End Function


Public Function GetProductResult_8167(rid As String) As Decimal
Dim __key = "8167" + rid
If (_engine._productResultCache.ContainsKey(__key)) = True Then Return _engine._productResultCache(__key)

 Dim result = 0
 
 Dim AcademyFilter As Decimal = GetProductResult_7582(rid) 
 Dim FundingBasis As String = _engine.GetDsDataValue(rid, 11187, "2017181")
 
 Print(AcademyFilter,"AcademyFilter",rid, 8167)
 Print(FundingBasis,"FundingBasis",rid, 8167)
 
 If FundingBasis = "Place" Then
 
 exclude(rid, 8167) 
 
 Else
 
 If AcademyFilter = 17181 Or AcademyFilter = 17182 Or AcademyFilter = 17183 then
 
 Dim P015_KS4_BESubtotal As Decimal = GetProductResult_8164(rid) 
 Dim Days_Open As Decimal = GetProductResult_8151(rid) 
 Dim Year_Days As Decimal = 365 
 
 Print(P015_KS4_BESubtotal,"P145 P015_KS4_BESubtotal",rid, 8167)
 Print(Days_Open,"Days Open",rid, 8167)
 Print(Year_Days,"Year Days",rid, 8167)
 
 Result = (P015_KS4_BESubtotal) *Divide(Days_Open, Year_Days)
 
 Else
 
 Exclude(rid, 8167) 
 
 End If 
 
 End If 
 
 
result = System.Math.Round(result, 6, MidpointRounding.AwayFromZero)
_engine._productResultCache.Add(__key, result)
Return result
 
End Function

Public Function GetProductResult_8169(rid As String) As Decimal
Dim __key = "8169" + rid
If (_engine._productResultCache.ContainsKey(__key)) = True Then Return _engine._productResultCache(__key)

 Dim result = 0
 
 Dim AcademyFilter As Decimal = GetProductResult_7582(rid) 
 Dim FundingBasis As String = _engine.GetDsDataValue(rid, 11187, "2017181")
 
 Print(AcademyFilter,"AcademyFilter",rid, 8169)
 Print(FundingBasis,"FundingBasis",rid, 8169)
 
 If FundingBasis = "Place" Then
 
 exclude(rid, 8169) 
 
 Else
 
 If AcademyFilter = 17181 Or AcademyFilter = 17182 Or AcademyFilter = 17183 then
 Dim ProviderName As String = _engine.GetDsDataValue(rid, 9070)
 Dim P021_PriFSMRate As Decimal = LAtoProv(_engine.GetLaDataValue(rid, 86450, Nothing)) 
 Dim LAMethod as string = LAtoProv(_engine.GetLaDataValue(rid, 86448, Nothing))
 
 If LAMethod = "FSM % Primary" then
 
 result = P021_PriFSMRate
 
 else
 
 result = 0

 end if

 Print(P021_PriFSMRate,"Pri FSM Per Pupil",rid, 8169) 
 Print(LAMethod, "FSM/FSM6?", rid, 8169)
 
 Else 
 
 exclude(rid, 8169)
 
 End if 
 
 End If
 
 
result = System.Math.Round(result, 6, MidpointRounding.AwayFromZero)
_engine._productResultCache.Add(__key, result)
Return result
 
End Function


Public Function GetProductResult_8168(rid As String) As Decimal
Dim __key = "8168" + rid
If (_engine._productResultCache.ContainsKey(__key)) = True Then Return _engine._productResultCache(__key)

 Dim result = 0
 
 Dim AcademyFilter As Decimal = GetProductResult_7582(rid) 
 Dim FundingBasis As String = _engine.GetDsDataValue(rid, 11187, "2017181")
 
 Print(AcademyFilter,"AcademyFilter",rid, 8168)
 Print(FundingBasis,"FundingBasis",rid, 8168)
 
 If FundingBasis = "Place" Then
 
 exclude(rid, 8168) 
 
 Else
 
 If AcademyFilter = 17181 Or AcademyFilter = 17182 Or AcademyFilter = 17183 then
 
 Dim FSMCensus As Decimal = _engine.GetDsDataValue(rid, 86896)
 Dim FSMAdj As Decimal = _engine.GetDsDataValue(rid, 86012)
 Dim FSMAdjString as string = _engine.GetDsDataValue(rid, 86012)
 Dim FSMPriCensusString as string = _engine.GetDsDataValue(rid, 86896)
 Dim LA_AV As Decimal = latoprov(_engine.GetLaDataValue(rid, 86809, Nothing))

 Print(LA_AV,"LA average", rid, 8168)
 Print(FSMCensus, "FSMPri Census", rid, 8168)
 Print(FSMAdj, "FSM Pri Adjusted", rid, 8168) 

 If string.IsNullOrEmpty(FSMPriCensusString) And string.IsNullOrEmpty(FSMAdjString) THEN 
 
 Result = LA_AV
 
 Else
 
 if string.IsNullOrEmpty(FSMAdjString) then
 
 result = FSMCensus
 
 else

 result = FSMAdj

 End if
 
 End if
 
 else
 
 exclude(rid, 8168)
 
 End If 
 
 End If
 
 
result = System.Math.Round(result, 6, MidpointRounding.AwayFromZero)
_engine._productResultCache.Add(__key, result)
Return result
 
End Function


Public Function GetProductResult_8170(rid As String) As Decimal
Dim __key = "8170" + rid
If (_engine._productResultCache.ContainsKey(__key)) = True Then Return _engine._productResultCache(__key)

 Dim result = 0
 
 Dim AcademyFilter As Decimal = GetProductResult_7582(rid) 
 Dim FundingBasis As String = _engine.GetDsDataValue(rid, 11187, "2017181")
 
 
 Print(AcademyFilter,"AcademyFilter",rid, 8170)
 Print(FundingBasis,"FundingBasis",rid, 8170)
 
 If FundingBasis = "Place" Then
 
 exclude(rid, 8170)
 
 Else

 
 
 
 
 
 
 
 
 End if
 
 print(FundingBasis, "FundingBasisType",rid, 8170)
 print(AcademyFilter, "AcademyFilter", rid, 8170)
 
 If AcademyFilter = 17181 Or (AcademyFilter = 17182 And FundingBasis = "Estimate") Or (AcademyFilter = 17183 And FundingBasis = "Estimate") then

 Dim ProviderName As String = _engine.GetDsDataValue(rid, 9070)
 Dim P020_PriFSMPupils As Decimal = GetProductResult_7667(rid) 
 Dim P021_PriFSMRate As Decimal = GetProductResult_8169(rid) 
 Dim P019_PriFSMFactor As Decimal = GetProductResult_8168(rid)  
 Dim P022_PriFSMSubtotal As Decimal = P020_PriFSMPupils * P021_PriFSMRate * P019_PriFSMFactor
 Dim Days_Open1516 As Decimal = GetProductResult_8151(rid) 
 
 Print(P020_PriFSMPupils,"P020_PriFSMPupils",rid, 8170)
 Print(P021_PriFSMRate,"P021_PriFSMRate",rid, 8170)
 Print(P019_PriFSMFactor,"P019_PriFSMFactor",rid, 8170) 
 
 result = P022_PriFSMSubtotal 
 
 else
 
 If (AcademyFilter = 17182 And FundingBasis = "Census") Or (AcademyFilter = 17183 And FundingBasis = "Census") then
 
 Dim FSMSelectedbyLA As String = LaToProv(_engine.GetLaDataValue(rid, 86448, Nothing))
 
 Print(FSMSelectedbyLA,"FSMSelectedbyLA",rid, 8170)
 
 If FSMSelectedbyLA = "FSM % Primary" then
 
 Dim P022_PriFSMSubtotalAPT As Decimal = _engine.GetDsDataValue(rid, 86098)
 
 Print(P022_PriFSMSubtotalAPT,"P022_PriFSMSubtotalAPT",rid, 8170)
 
 Result = P022_PriFSMSubtotalAPT
 
 Else
 
 Result = 0
 
 End If
 Else 
 
 exclude(rid, 8170)
 
 End If
 
 End if
 
 
 
 
result = System.Math.Round(result, 2, MidpointRounding.AwayFromZero)
_engine._productResultCache.Add(__key, result)
Return result
 
End Function


Public Function GetProductResult_8171(rid As String) As Decimal
Dim __key = "8171" + rid
If (_engine._productResultCache.ContainsKey(__key)) = True Then Return _engine._productResultCache(__key)

 Dim result = 0
 
 Dim AcademyFilter As Decimal = GetProductResult_7582(rid) 
 Dim FundingBasis As String = _engine.GetDsDataValue(rid, 11187, "2017181")
 
 Print(AcademyFilter,"AcademyFilter",rid, 8171)
 Print(FundingBasis,"FundingBasis",rid, 8171)
 
 If FundingBasis = "Place" Then
 
 exclude(rid, 8171) 
 
 Else

 If AcademyFilter = 17181 Or AcademyFilter = 17182 Or AcademyFilter = 17183 then

 Dim P022_PriFSMSubtotal As Decimal = GetProductResult_8170(rid) 
 Dim Days_Open As Decimal = GetProductResult_8151(rid) 
 Dim Year_Days As Decimal = 365
 
 print(Days_Open, "Days_Open",rid, 8171) 
 print(Year_Days, "Year_Days",rid, 8171) 
 
 Result = (P022_PriFSMSubtotal) *Divide(Days_Open, Year_Days)
 
 Else 
 
 Exclude(rid, 8171)
 
 End If 
 
 End If
 
 
result = System.Math.Round(result, 6, MidpointRounding.AwayFromZero)
_engine._productResultCache.Add(__key, result)
Return result
 
End Function


 Public Function GetProductResult_8173(rid As String) As Decimal
Dim __key = "8173" + rid
If (_engine._productResultCache.ContainsKey(__key)) = True Then Return _engine._productResultCache(__key)

    Dim result = 0
    
    Dim AcademyFilter As Decimal = GetProductResult_7582(rid) 
    Dim FundingBasis As String = _engine.GetDsDataValue(rid, 11187, "2017181")
     
    Print(AcademyFilter,"AcademyFilter",rid, 8173)
    Print(FundingBasis,"FundingBasis",rid, 8173)
    
    If FundingBasis = "Place" Then
    
    exclude(rid, 8173)
    
    Else
    
        If AcademyFilter = 17181 Or AcademyFilter = 17182 Or AcademyFilter = 17183 then
        
        Dim provider As String = _engine.GetDsDataValue(rid, 9070)
        Dim PriFSMRate As Decimal = LAtoProv(_engine.GetLaDataValue(rid, 86450, Nothing)) 
        Dim LAMethod as string = LAtoProv(_engine.GetLaDataValue(rid, 86448, Nothing))
    
            If LAMethod = "FSM6 % Primary" then
   
            result = PriFSMRate
   
            else
   
            result = 0

            end if

            Print(PriFSMRate,"Pri FSM Per Pupil",rid, 8173)     
            Print(LAMethod, "FSM/FSM6?", rid, 8173)
          
            else
            
            exclude(rid, 8173)
            
            End if
            
        End If  
        
        
result = System.Math.Round(result, 6, MidpointRounding.AwayFromZero)
_engine._productResultCache.Add(__key, result)
Return result
    
End Function



 Public Function GetProductResult_8172(rid As String) As Decimal
Dim __key = "8172" + rid
If (_engine._productResultCache.ContainsKey(__key)) = True Then Return _engine._productResultCache(__key)

    Dim result = 0
    
    Dim AcademyFilter As Decimal = GetProductResult_7582(rid) 
    Dim FundingBasis As String = _engine.GetDsDataValue(rid, 11187, "2017181")
     
    Print(AcademyFilter,"AcademyFilter",rid, 8172)
    Print(FundingBasis,"FundingBasis",rid, 8172)
    
    If FundingBasis = "Place" Then
    
    exclude(rid, 8172) 
    
    Else
     
        If AcademyFilter = 17181 Or AcademyFilter = 17182 Or AcademyFilter = 17183 then
        
        Dim FSMCensus As Decimal = _engine.GetDsDataValue(rid, 86898)
        Dim FSMAdj As Decimal = _engine.GetDsDataValue(rid, 86014)
        Dim FSMAdjString as string = _engine.GetDsDataValue(rid, 86014)
        Dim FSMPriCensusString as string  = _engine.GetDsDataValue(rid, 86898)
        Dim LA_AV As Decimal = latoprov(_engine.GetLaDataValue(rid, 86811, Nothing))

        Print(LA_AV,"LA average", rid, 8172)
        Print(FSMCensus, "FSM6 Pri Census", rid, 8172)
        Print(FSMAdj, "FSM6 Pri Adjusted", rid, 8172)  

            If string.IsNullOrEmpty(FSMPriCensusString) And string.IsNullOrEmpty(FSMAdjString) THEN
            
            Result = LA_AV
            
            Else
                                                      
                if string.IsNullOrEmpty(FSMAdjString) then
                
                result = FSMCensus
  
                else

                result = FSMAdj

                End if
                
            End if
            
            else
            
            exclude(rid, 8172)
            
        End If
        
    End If
    
    
result = System.Math.Round(result, 6, MidpointRounding.AwayFromZero)
_engine._productResultCache.Add(__key, result)
Return result
    
End Function


 Public Function GetProductResult_8174(rid As String) As Decimal
Dim __key = "8174" + rid
If (_engine._productResultCache.ContainsKey(__key)) = True Then Return _engine._productResultCache(__key)

    Dim result = 0

    Dim AcademyFilter As Decimal = GetProductResult_7582(rid) 
    Dim FundingBasis As String = _engine.GetDsDataValue(rid, 11187, "2017181")
   
     
    Print(AcademyFilter,"AcademyFilter",rid, 8174)
    Print(FundingBasis,"FundingBasis",rid, 8174)
    
    If FundingBasis = "Place" Then
    
    exclude(rid, 8174)   
    
    Else
    
       
        
       
                
           
            
           
          
            End if
    
            print(FundingBasis, "FundingBasisType",rid, 8174)
            print(AcademyFilter, "AcademyFilter", rid, 8174)
            
                If AcademyFilter = 17181 Or (AcademyFilter = 17182 And FundingBasis = "Estimate") Or (AcademyFilter = 17183 And FundingBasis = "Estimate")   then

                Dim P020_PriFSMPupils As Decimal = GetProductResult_7667(rid) 
                Dim P026_PriFSM6Rate As Decimal =   GetProductResult_8173(rid) 
                Dim P024_PriFSM6Factor As Decimal = GetProductResult_8172(rid) 
                Dim P027_PriFSM6Subtotal As Decimal =  P020_PriFSMPupils * P026_PriFSM6Rate* P024_PriFSM6Factor
     
   
                Print(P020_PriFSMPupils,"P020_PriFSMPupils",rid, 8174)
                print(P026_PriFSM6Rate, "P026_PriFSM6Rate",rid, 8174)
                Print(P024_PriFSM6Factor,"P024_PriFSM6Factor",rid, 8174)                                 

                result = P027_PriFSM6Subtotal 
         
                else
      
                    If  (AcademyFilter = 17182 And FundingBasis = "Census") Or (AcademyFilter = 17183 And FundingBasis = "Census") then
                   
                    Dim FSMSelectedbyLA As String = LaToProv(_engine.GetLaDataValue(rid, 86448, Nothing))
                     
                    Print(FSMSelectedbyLA,"FSMSelectedbyLA",rid, 8174)
                        
                            If FSMSelectedbyLA = "FSM6 % Primary" then
                            
                            Dim P027_PriFSM6SubtotalAPT As Decimal = _engine.GetDsDataValue(rid, 86098)
                            
                            Print(P027_PriFSM6SubtotalAPT,"P027_PriFSM6SubtotalAPT",rid, 8174)
    
                            Result = P027_PriFSM6SubtotalAPT
                            
                            Else
                            
                            Result = 0
                            
                            End If
                    Else
                    
                    exclude(rid, 8174)
                    
                    End If
  
                End If 
            
       
    
    
result = System.Math.Round(result, 2, MidpointRounding.AwayFromZero)
_engine._productResultCache.Add(__key, result)
Return result
    
End Function


Public Function GetProductResult_8177(rid As String) As Decimal
Dim __key = "8177" + rid
If (_engine._productResultCache.ContainsKey(__key)) = True Then Return _engine._productResultCache(__key)

    Dim result = 0
    
    Dim AcademyFilter As Decimal = GetProductResult_7582(rid) 
    Dim FundingBasis As String = _engine.GetDsDataValue(rid, 11187, "2017181")
     
    Print(AcademyFilter,"AcademyFilter",rid, 8177)
    Print(FundingBasis,"FundingBasis",rid, 8177)
    
    If FundingBasis = "Place" Then
    
    exclude(rid, 8177) 
    
    Else

        If AcademyFilter = 17181 Or AcademyFilter = 17182 Or AcademyFilter = 17183 then

        Dim P027_PriFSM6Subtotal As Decimal = GetProductResult_8174(rid) 
        Dim Days_Open As Decimal = GetProductResult_8151(rid) 
        Dim Year_Days As Decimal = 365
        
        print(Days_Open, "Days_Open",rid, 8177)                          
        print(Year_Days, "Year_Days",rid, 8177)
        
        Result = (P027_PriFSM6Subtotal) *Divide(Days_Open, Year_Days)
    
        Else  
        
        Exclude(rid, 8177) 
        
       End If  
       
    End If
    
    
result = System.Math.Round(result, 6, MidpointRounding.AwayFromZero)
_engine._productResultCache.Add(__key, result)
Return result
    
End Function


Public Function GetProductResult_7673(rid As String) As Decimal
Dim __key = "7673" + rid
If (_engine._productResultCache.ContainsKey(__key)) = True Then Return _engine._productResultCache(__key)

 Dim result = 0
 
 
 
 
 
 Dim AcadFilter As Decimal = GetProductResult_7582(rid) 
 Dim NOR_KS3_SBS As Decimal = GetProductResult_7669(rid) 
 Dim NOR_KS4_SBS As Decimal = GetProductResult_7671(rid) 
 
 Print(NOR_KS3_SBS, "KS3", rid, 7673)
 Print(NOR_KS4_SBS, "KS4", rid, 7673) 
 
If currentscenario.periodid = 2017181 And (AcadFilter = 17181 Or AcadFilter = 17182 Or AcadFilter = 17183) then 
 Result = NOR_KS3_SBS + NOR_KS4_SBS
 Else Exclude(rid, 7673)
End If 
 
 
_engine._productResultCache.Add(__key, result)
Return result
End Function


Public Function GetProductResult_8179(rid As String) As Decimal
Dim __key = "8179" + rid
If (_engine._productResultCache.ContainsKey(__key)) = True Then Return _engine._productResultCache(__key)

    Dim result = 0
    
    Dim AcademyFilter As Decimal = GetProductResult_7582(rid) 
    Dim FundingBasis As String = _engine.GetDsDataValue(rid, 11187, "2017181")
     
    Print(AcademyFilter,"AcademyFilter",rid, 8179)
    Print(FundingBasis,"FundingBasis",rid, 8179)
    
    If FundingBasis = "Place" Then

    exclude(rid, 8179) 
    
    Else
   
        If AcademyFilter = 17181 Or AcademyFilter = 17182 Or AcademyFilter = 17183 then
        
        Dim SecFSMRate As Decimal = LAtoProv(_engine.GetLaDataValue(rid, 86460, Nothing)) 
        Dim LAMethod as string = LAtoProv(_engine.GetLaDataValue(rid, 86458, Nothing))
    
            If LAMethod = "FSM % Secondary" then
   
            result = SecFSMRate
   
            else
   
            result = 0

            end if

            Print(SecFSMRate,"Sec FSM Per Pupil",rid, 8179)     
            Print(LAMethod, "FSM/FSM6?", rid, 8179)
       
            else
            
            exclude(rid, 8179)
            
        End if
            
    End If 
    
    
result = System.Math.Round(result, 6, MidpointRounding.AwayFromZero)
_engine._productResultCache.Add(__key, result)
Return result
        
End Function


Public Function GetProductResult_8178(rid As String) As Decimal
Dim __key = "8178" + rid
If (_engine._productResultCache.ContainsKey(__key)) = True Then Return _engine._productResultCache(__key)

    Dim result = 0
    
    Dim AcademyFilter As Decimal = GetProductResult_7582(rid) 
    Dim FundingBasis As String = _engine.GetDsDataValue(rid, 11187, "2017181")
     
    Print(AcademyFilter,"AcademyFilter",rid, 8178)
    Print(FundingBasis,"FundingBasis",rid, 8178)
    
    If FundingBasis = "Place" Then
    
    exclude(rid, 8178) 
    
    Else
    
        If AcademyFilter = 17181 Or AcademyFilter = 17182 Or AcademyFilter = 17183 then
        
        Dim FSMCensus As Decimal = _engine.GetDsDataValue(rid, 86900)
        Dim FSMAdj As Decimal = _engine.GetDsDataValue(rid, 86016)
        Dim FSMAdjString as string = _engine.GetDsDataValue(rid, 86016)
        Dim FSMCensusCensusString as string  = _engine.GetDsDataValue(rid, 86900)
        Dim LA_AV As Decimal = latoprov(_engine.GetLaDataValue(rid, 86813, Nothing))

        Print(LA_AV, "LA average", rid, 8178)
        Print(FSMCensus, "FSM Sec Census", rid, 8178)
        Print(FSMAdj, "FSMAdj", rid, 8178)  

            If string.IsNullOrEmpty(FSMCensusCensusString) And string.IsNullOrEmpty(FSMAdjString) THEN 
            
            Result = LA_AV
            
            Else
                                                         
                if string.IsNullOrEmpty(FSMAdjString) then
                
                result = FSMCensus
  
                else

                result = FSMAdj

                End if
                
            End if
            
            else
            
            exclude(rid, 8178)
            
        End if
        
    End If
    
    
result = System.Math.Round(result, 6, MidpointRounding.AwayFromZero)
_engine._productResultCache.Add(__key, result)
Return result
    
End Function
 

Public Function GetProductResult_8180(rid As String) As Decimal
Dim __key = "8180" + rid
If (_engine._productResultCache.ContainsKey(__key)) = True Then Return _engine._productResultCache(__key)

    Dim result = 0
    
    Dim AcademyFilter As Decimal = GetProductResult_7582(rid) 
    Dim FundingBasis As String = _engine.GetDsDataValue(rid, 11187, "2017181")
   
        
    Print(AcademyFilter,"AcademyFilter",rid, 8180)
    Print(FundingBasis,"FundingBasis",rid, 8180)
    
    If FundingBasis = "Place" Then
    
    exclude(rid, 8180) 
    
    Else
        
       
        
       
                
           
            
           
          
            End if
            
            print(FundingBasis, "FundingBasisType",rid, 8180)
            print(AcademyFilter, "AcademyFilter", rid, 8180)
            
                If AcademyFilter = 17181 Or (AcademyFilter = 17182 And FundingBasis = "Estimate") Or (AcademyFilter = 17183 And FundingBasis = "Estimate")   then

                Dim P031_SecFSMPupils As Decimal = GetProductResult_7673(rid) 
                Dim P032_SecFSMRate As Decimal =   GetProductResult_8179(rid) 
                Dim P030_SecFSMFactor As Decimal = GetProductResult_8178(rid) 
                Dim P033_SecFSMSubtotal As Decimal =  P031_SecFSMPupils * P032_SecFSMRate * P030_SecFSMFactor  
   
                Print(P031_SecFSMPupils,"P031_SecFSMPupils",rid, 8180)
                Print(P032_SecFSMRate,"P032_SecFSMRate",rid, 8180)
                Print(P030_SecFSMFactor,"P030_SecFSMFactor",rid, 8180)                                 

                result = P033_SecFSMSubtotal 
    
                else
             
                    If  (AcademyFilter = 17182 And FundingBasis = "Census") Or (AcademyFilter = 17183 And FundingBasis = "Census") then
                    
                            Dim FSMSelectedbyLA As String = LaToProv(_engine.GetLaDataValue(rid, 86458, Nothing))                        
                            
                            Print(FSMSelectedbyLA,"FSMSelectedbyLA",rid, 8180)
                        
                                If FSMSelectedbyLA = "FSM % Secondary" then
                                
                                Dim P033_SecFSMSubtotalAPT As Decimal = _engine.GetDsDataValue(rid, 86100)
                                
                                Print(P033_SecFSMSubtotalAPT,"P033_SecFSMSubtotalAPT",rid, 8180)
    
                                Result = P033_SecFSMSubtotalAPT
                                
                                Else
                                
                                Result = 0
                                
                                End If
                                
                    Else
                    
                    exclude(rid, 8180)
                    
                    End If  

                End If 
                
    
     
    
result = System.Math.Round(result, 2, MidpointRounding.AwayFromZero)
_engine._productResultCache.Add(__key, result)
Return result
    
End Function


 Public Function GetProductResult_8181(rid As String) As Decimal
Dim __key = "8181" + rid
If (_engine._productResultCache.ContainsKey(__key)) = True Then Return _engine._productResultCache(__key)

    Dim result = 0
    
    Dim AcademyFilter As Decimal = GetProductResult_7582(rid) 
    Dim FundingBasis As String = _engine.GetDsDataValue(rid, 11187, "2017181")
     
    Print(AcademyFilter,"AcademyFilter",rid, 8181)
    Print(FundingBasis,"FundingBasis",rid, 8181)
    
    If FundingBasis = "Place" Then
    
    exclude(rid, 8181) 
    
    Else

        If AcademyFilter = 17181 Or AcademyFilter = 17182 Or AcademyFilter = 17183 then

        Dim P033_SecFSMSubtotal As Decimal = GetProductResult_8180(rid) 
        Dim Days_Open As Decimal = GetProductResult_8151(rid) 
        Dim Year_Days As Decimal = 365
        
        print(Days_Open, "Days_Open",rid, 8181)                          
        print(Year_Days, "Year_Days",rid, 8181)                           

        Result = (P033_SecFSMSubtotal) *Divide(Days_Open, Year_Days)
    
        Else 
        
        Exclude(rid, 8181)  
        
        End If  
        
    End If
    
    
result = System.Math.Round(result, 6, MidpointRounding.AwayFromZero)
_engine._productResultCache.Add(__key, result)
Return result

End Function


  Public Function GetProductResult_8183(rid As String) As Decimal
Dim __key = "8183" + rid
If (_engine._productResultCache.ContainsKey(__key)) = True Then Return _engine._productResultCache(__key)

    Dim result = 0
    
    Dim AcademyFilter As Decimal = GetProductResult_7582(rid) 
    Dim FundingBasis As String = _engine.GetDsDataValue(rid, 11187, "2017181")
     
    Print(AcademyFilter,"AcademyFilter",rid, 8183)
    Print(FundingBasis,"FundingBasis",rid, 8183)
    
    If FundingBasis = "Place" Then
    
    exclude(rid, 8183)
    
    Else

        If AcademyFilter = 17181 Or AcademyFilter = 17182 Or AcademyFilter = 17183 then
        
        Dim SecFSMRate As Decimal = LAtoProv(_engine.GetLaDataValue(rid, 86460, Nothing)) 
        Dim LAMethod as string = LAtoProv(_engine.GetLaDataValue(rid, 86458, Nothing))
    
            If LAMethod = "FSM6 % Secondary" then
     
            result = SecFSMRate
   
            else
   
            result = 0

            end if

            Print(SecFSMRate,"Sec FSM Per Pupil",rid, 8183)     
            Print(LAMethod, "FSM/FSM6?", rid, 8183)
       
            else
            
            exclude(rid, 8183)
            
        End if
        
    End If
    
    
result = System.Math.Round(result, 6, MidpointRounding.AwayFromZero)
_engine._productResultCache.Add(__key, result)
Return result
    
End Function














 Public Function GetProductResult_8182(rid As String) As Decimal
Dim __key = "8182" + rid
If (_engine._productResultCache.ContainsKey(__key)) = True Then Return _engine._productResultCache(__key)

    Dim result = 0
    
    Dim AcademyFilter As Decimal = GetProductResult_7582(rid) 
    Dim FundingBasis As String = _engine.GetDsDataValue(rid, 11187, "2017181")
     
    Print(AcademyFilter,"AcademyFilter",rid, 8182)
    Print(FundingBasis,"FundingBasis",rid, 8182)
    
    If FundingBasis = "Place" Then
    
    exclude(rid, 8182) 
    
    Else
    
        If AcademyFilter = 17181 Or AcademyFilter = 17182 Or AcademyFilter = 17183 then
        
        Dim FSMCensus As Decimal = _engine.GetDsDataValue(rid, 86902)
        Dim FSMAdj As Decimal = _engine.GetDsDataValue(rid, 86018)
        Dim FSMAdjString as string = _engine.GetDsDataValue(rid, 86018)
        Dim FSMCensusString as string = _engine.GetDsDataValue(rid, 86902)
        Dim LA_AV As Decimal = latoprov(_engine.GetLaDataValue(rid, 86815, Nothing))

        Print(LA_AV,"LA average", rid, 8182)
        Print(FSMCensus, "FSMCensus", rid, 8182)
        Print(FSMAdj, "FSMAdj", rid, 8182)  

            If string.IsNullOrEmpty(FSMCensusString) And string.IsNullOrEmpty(FSMAdjString) THEN 
            
            Result = LA_AV
            
            Else
                                                         
                if string.IsNullOrEmpty(FSMAdjString) then
                
                result = FSMCensus
  
                else

                result = FSMAdj

                End if
                
            End if
            
            Else
            
            exclude(rid, 8182)
            
        End if
        
    End If
    
    
result = System.Math.Round(result, 6, MidpointRounding.AwayFromZero)
_engine._productResultCache.Add(__key, result)
Return result
    
End Function


Public Function GetProductResult_8184(rid As String) As Decimal
Dim __key = "8184" + rid
If (_engine._productResultCache.ContainsKey(__key)) = True Then Return _engine._productResultCache(__key)

    Dim result = 0
    
    Dim AcademyFilter As Decimal = GetProductResult_7582(rid) 
    Dim FundingBasis As String = _engine.GetDsDataValue(rid, 11187, "2017181")
   
      
    Print(AcademyFilter,"AcademyFilter",rid, 8184)
    Print(FundingBasis,"FundingBasis",rid, 8184)
    
    If FundingBasis = "Place" Then
    
    exclude(rid, 8184) 
    
    Else

       
        
       
                
           
            
           
          
            End if
            
            print(FundingBasis, "FundingBasisType",rid, 8184)
            print(AcademyFilter, "AcademyFilter", rid, 8184)

                If AcademyFilter = 17181 Or (AcademyFilter = 17182 And FundingBasis = "Estimate") Or (AcademyFilter = 17183 And FundingBasis = "Estimate")   then

                Dim P036_SecFSM6Pupils As Decimal = GetProductResult_7673(rid) 
                Dim P037_SecFSM6Rate As Decimal =   GetProductResult_8183(rid) 
                Dim P035_SecFSM6Factor As Decimal = GetProductResult_8182(rid) 
                Dim P038_SecFSM6Subtotal As Decimal =  P036_SecFSM6Pupils * P037_SecFSM6Rate * P035_SecFSM6Factor
   
                Print(P036_SecFSM6Pupils ,"P036_SecFSM6Pupils",rid, 8184)
                Print(P037_SecFSM6Rate ,"P037_SecFSM6Rate",rid, 8184)
                Print(P035_SecFSM6Factor,"P035_SecFSM6Factor",rid, 8184)                                 

                result = P038_SecFSM6Subtotal 
        
                else

                    If  (AcademyFilter = 17182 And FundingBasis = "Census") Or (AcademyFilter = 17183 And FundingBasis = "Census") then
                    
                    Dim FSMSelectedbyLA As String = LaToProv(_engine.GetLaDataValue(rid, 86458, Nothing))                        
                    
                    Print(FSMSelectedbyLA,"FSMSelectedbyLA",rid, 8184)
                        
                        If FSMSelectedbyLA = "FSM6 % Secondary" then
                        
                        Dim P038_SecFSM6SubtotalAPT As Decimal = _engine.GetDsDataValue(rid, 86100) 
                            
                        Print(P038_SecFSM6SubtotalAPT,"P038_SecFSM6SubtotalAPT",rid, 8184)
                              
                        Result = P038_SecFSM6SubtotalAPT
                        
                        Else
                        
                        Result = 0
                        
                        End If
                        
                    Else 
                    
                    exclude(rid, 8184)
                    
                    End If  
                    
                End if
                
           
            
    
result = System.Math.Round(result, 2, MidpointRounding.AwayFromZero)
_engine._productResultCache.Add(__key, result)
Return result
    
End Function


Public Function GetProductResult_8187(rid As String) As Decimal
Dim __key = "8187" + rid
If (_engine._productResultCache.ContainsKey(__key)) = True Then Return _engine._productResultCache(__key)

    Dim result = 0
    
    Dim AcademyFilter As Decimal = GetProductResult_7582(rid) 
    Dim FundingBasis As String = _engine.GetDsDataValue(rid, 11187, "2017181")
     
    Print(AcademyFilter,"AcademyFilter",rid, 8187)
    Print(FundingBasis,"FundingBasis",rid, 8187)
    
    If FundingBasis = "Place" Then
    
    exclude(rid, 8187) 
    
    Else
   
        If AcademyFilter = 17181 Or AcademyFilter = 17182 Or AcademyFilter = 17183 then

        Dim P038_SecFSM6Subtotal As Decimal = GetProductResult_8184(rid) 
        Dim Days_Open As Decimal = GetProductResult_8151(rid) 
        Dim Year_Days As Decimal = 365
        
        print(Days_Open, "Days_Open",rid, 8187)                          
        print(Year_Days, "Year_Days",rid, 8187)  
        print(P038_SecFSM6Subtotal,"P038_SecFSM6Subtotal",rid, 8187)     

        Result = (P038_SecFSM6Subtotal) *Divide(Days_Open, Year_Days)
    
        Else  
        
        Exclude(rid, 8187)
        
        End If  
        
    End If
    
    
result = System.Math.Round(result, 6, MidpointRounding.AwayFromZero)
_engine._productResultCache.Add(__key, result)
Return result
    
End Function


 Public Function GetProductResult_8189(rid As String) As Decimal
Dim __key = "8189" + rid
If (_engine._productResultCache.ContainsKey(__key)) = True Then Return _engine._productResultCache(__key)

    Dim result = 0
    
    Dim AcademyFilter As Decimal = GetProductResult_7582(rid) 
    Dim FundingBasis As String = _engine.GetDsDataValue(rid, 11187, "2017181")
     
    Print(AcademyFilter,"AcademyFilter",rid, 8189)
    Print(FundingBasis,"FundingBasis",rid, 8189)
    
    If FundingBasis = "Place" Then
    
    exclude(rid, 8189)
    
    Else
    
        If AcademyFilter = 17181 Or AcademyFilter = 17182 Or AcademyFilter = 17183 then
        
        Dim ProviderName as string = _engine.GetDsDataValue(rid, 9070)
        Dim P043_IDACI1PriRate As Decimal = LAtoProv(_engine.GetLaDataValue(rid, 86468, Nothing)) 
    
        Print(ProviderName,"Provider Name",rid, 8189)
        
        Result = P043_IDACI1PriRate
        
        else
        
        exclude(rid, 8189)
        
        End if
        
    End if
    

result = System.Math.Round(result, 6, MidpointRounding.AwayFromZero)
_engine._productResultCache.Add(__key, result)
Return result

End Function


 Public Function GetProductResult_8188(rid As String) As Decimal
Dim __key = "8188" + rid
If (_engine._productResultCache.ContainsKey(__key)) = True Then Return _engine._productResultCache(__key)

    Dim result = 0
    
    Dim AcademyFilter As Decimal = GetProductResult_7582(rid) 
    Dim FundingBasis As String = _engine.GetDsDataValue(rid, 11187, "2017181")
     
    Print(AcademyFilter,"AcademyFilter",rid, 8188)
    Print(FundingBasis,"FundingBasis",rid, 8188)
    
    If FundingBasis = "Place" Then
    
    exclude(rid, 8188) 
    
    Else

        If AcademyFilter = 17181 Or AcademyFilter = 17182 Or AcademyFilter = 17183 then
        
        Dim IDACI1PriCensus As Decimal = _engine.GetDsDataValue(rid, 86906)
        Dim IDACI1PriAdj As Decimal = _engine.GetDsDataValue(rid, 86022)
        Dim IDACI1PriAdjString as string = _engine.GetDsDataValue(rid, 86022)
        Dim IDACI1PriCensusString As string = _engine.GetDsDataValue(rid, 86906)
        Dim LA_AV As Decimal = latoprov(_engine.GetLaDataValue(rid, 86819, Nothing))
    
        Print(LA_AV,"LA average",rid, 8188)
        Print(IDACI1PriCensus,"IDACI 1 Pri Census", rid, 8188)
        Print(IDACI1PriAdj,"IDACI 1 Pri Adjusted", rid, 8188) 
    
            If string.IsNullOrEmpty(IDACI1PriCensusString) And string.IsNullOrEmpty(IDACI1PriAdjString) THEN
            
            Result = LA_AV
            
            ELSE
    
                If string.IsNullOrEmpty(IDACI1PriAdjString) THEN
                
                Result = IDACI1PriCensus
                
                Else
                
                Result = IDACI1PriAdj
                
                End if
            
            End if
            
            Else
            
            exclude(rid, 8188)
            
        End if
        
    End if
    
    
result = System.Math.Round(result, 6, MidpointRounding.AwayFromZero)
_engine._productResultCache.Add(__key, result)
Return result
    
End Function























Public Function GetProductResult_8190(rid As String) As Decimal
Dim __key = "8190" + rid
If (_engine._productResultCache.ContainsKey(__key)) = True Then Return _engine._productResultCache(__key)

    Dim result = 0
    
    Dim AcademyFilter As Decimal = GetProductResult_7582(rid) 
    Dim FundingBasis As String = _engine.GetDsDataValue(rid, 11187, "2017181")
   
    
    Print(AcademyFilter,"AcademyFilter",rid, 8190)
    Print(FundingBasis,"FundingBasis",rid, 8190)
    
        If FundingBasis = "Place" Then
        
        exclude(rid, 8190)
        
        Else
               
           
            
           
                
               
                
               
          
                End if
    
                print(FundingBasis, "FundingBasisType",rid, 8190)
                print(AcademyFilter, "AcademyFilter", rid, 8190)
      
                    If AcademyFilter = 17181 Or (AcademyFilter = 17182 And FundingBasis = "Estimate") Or (AcademyFilter = 17183 And FundingBasis = "Estimate")   then

                    Dim ProviderName as string = _engine.GetDsDataValue(rid, 9070)
                    Dim P042_IDACI1PriPupils As Decimal = GetProductResult_7667(rid) 
                    Dim P043_IDACI1PriRate As Decimal = GetProductResult_8189(rid) 
                    Dim P041_IDACI1PriFactor As Decimal = GetProductResult_8188(rid) 
                    Dim P044_IDACI1PriSubtotal As Decimal = P042_IDACI1PriPupils * P043_IDACI1PriRate * P041_IDACI1PriFactor
        
                    Print(ProviderName,"Provider Name",rid, 8190)
                    Print(P042_IDACI1PriPupils,"P042_IDACI1PriPupils",rid, 8190)
                    Print(P043_IDACI1PriRate,"P043_IDACI1PriRate",rid, 8190)
                    Print(P041_IDACI1PriFactor,"P041_IDACI1PriFactor",rid, 8190)                                 

                    Result = P044_IDACI1PriSubtotal  
    
                    else

                        If (AcademyFilter = 17182 And FundingBasis = "Census") Or (AcademyFilter = 17183 And FundingBasis = "Census") then
                        
                        Dim P044_IDACI1PriSubtotalAPT As Decimal = _engine.GetDsDataValue(rid, 86104)
                         
                        Print(P044_IDACI1PriSubtotalAPT,"P044_IDACI1PriSubtotalAPT",rid, 8190)
    
                        Result = P044_IDACI1PriSubtotalAPT
                       
                        Else  
                        
                        exclude(rid, 8190)
                        
                        End If  
                        
                    End If  
           
    
    
result = System.Math.Round(result, 2, MidpointRounding.AwayFromZero)
_engine._productResultCache.Add(__key, result)
Return result
    
End Function


 Public Function GetProductResult_8193(rid As String) As Decimal
Dim __key = "8193" + rid
If (_engine._productResultCache.ContainsKey(__key)) = True Then Return _engine._productResultCache(__key)

    Dim result = 0
    
    Dim AcademyFilter As Decimal = GetProductResult_7582(rid) 
    Dim FundingBasis As String = _engine.GetDsDataValue(rid, 11187, "2017181")
     
    Print(AcademyFilter,"AcademyFilter",rid, 8193)
    Print(FundingBasis,"FundingBasis",rid, 8193)
    
    If FundingBasis = "Place" Then
    
    exclude(rid, 8193) 
    
    Else

        If AcademyFilter = 17181 Or AcademyFilter = 17182 Or AcademyFilter = 17183 then
      
        Dim P044_IDACI1PriSubtotal As Decimal = GetProductResult_8190(rid) 
        Dim Days_Open As Decimal = GetProductResult_8151(rid) 
        Dim Year_Days As Decimal = 365                     

        Print(P044_IDACI1PriSubtotal,"P044 IDACI1PriSubtotal",rid, 8193)
        Print(Days_Open,"Days Open",rid, 8193)
        Print(Year_Days,"Year Days",rid, 8193)
    
        Result = (P044_IDACI1PriSubtotal) *Divide(Days_Open, Year_Days)
    
        Else
        
        Exclude(rid, 8193)
        
        End If
        
    End if
    
    
result = System.Math.Round(result, 6, MidpointRounding.AwayFromZero)
_engine._productResultCache.Add(__key, result)
Return result
    
End Function



Public Function GetProductResult_8195(rid As String) As Decimal
Dim __key = "8195" + rid
If (_engine._productResultCache.ContainsKey(__key)) = True Then Return _engine._productResultCache(__key)

    Dim result = 0
    
    Dim AcademyFilter As Decimal = GetProductResult_7582(rid) 
    Dim FundingBasis As String = _engine.GetDsDataValue(rid, 11187, "2017181")
     
    Print(AcademyFilter,"AcademyFilter",rid, 8195)
    Print(FundingBasis,"FundingBasis",rid, 8195)
    
    If FundingBasis = "Place" Then
    
    exclude(rid, 8195) 
    
    Else

        If AcademyFilter = 17181 Or AcademyFilter = 17182 Or AcademyFilter = 17183 then
        
        Dim ProviderName As String = _engine.GetDsDataValue(rid, 9070)
        Dim P049_IDACI2PriRate As Decimal = LAtoProv(_engine.GetLaDataValue(rid, 86482, Nothing)) 
    
        Print(ProviderName,"Provider Name",rid, 8195)    
        
        Result = P049_IDACI2PriRate
    
        else
        
        exclude(rid, 8195)
        
        End if
        
    End if
    
    
result = System.Math.Round(result, 6, MidpointRounding.AwayFromZero)
_engine._productResultCache.Add(__key, result)
Return result
    
End Function




Public Function GetProductResult_8194(rid As String) As Decimal
Dim __key = "8194" + rid
If (_engine._productResultCache.ContainsKey(__key)) = True Then Return _engine._productResultCache(__key)

    Dim result = 0
    
    Dim AcademyFilter As Decimal = GetProductResult_7582(rid) 
    Dim FundingBasis As String = _engine.GetDsDataValue(rid, 11187, "2017181")
     
    Print(AcademyFilter,"AcademyFilter",rid, 8194)
    Print(FundingBasis,"FundingBasis",rid, 8194)
    
    If FundingBasis = "Place" Then
    
    exclude(rid, 8194)
    
    Else

        If AcademyFilter = 17181 Or AcademyFilter = 17182 Or AcademyFilter = 17183 then
        
        Dim IDACI2PriCensus As Decimal = _engine.GetDsDataValue(rid, 86908)
        Dim IDACI2PriAdj As Decimal = _engine.GetDsDataValue(rid, 86024)
        Dim IDACI2PriAdjString as string = _engine.GetDsDataValue(rid, 86024)
        Dim IDACI2PriCensusString As string = _engine.GetDsDataValue(rid, 86908)
        Dim LA_AV As Decimal = latoprov(_engine.GetLaDataValue(rid, 86821, Nothing))
    
        Print(LA_AV,"LA average",rid, 8194)
        Print(IDACI2PriCensus,"IDACI 2 Pri Census", rid, 8194)
        Print(IDACI2PriAdj,"IDACI 2 Pri Adjusted", rid, 8194) 

            If string.IsNullOrEmpty(IDACI2PriCensusString) And string.IsNullOrEmpty(IDACI2PriAdjString)  THEN
            
            Result = LA_AV
            
            ELSE
    
                If string.IsNullOrEmpty(IDACI2PriAdjString) THEN
                
                Result = IDACI2PriCensus
                
                Else
                
                Result = IDACI2PriAdj
                    
                End if

            End if
            
            else
            
            exclude(rid, 8194)
            
        End if
        
    End if
    
    
result = System.Math.Round(result, 6, MidpointRounding.AwayFromZero)
_engine._productResultCache.Add(__key, result)
Return result
    
End Function


 Public Function GetProductResult_8196(rid As String) As Decimal
Dim __key = "8196" + rid
If (_engine._productResultCache.ContainsKey(__key)) = True Then Return _engine._productResultCache(__key)

    Dim result = 0
    
    Dim AcademyFilter As Decimal = GetProductResult_7582(rid) 
    Dim FundingBasis As String = _engine.GetDsDataValue(rid, 11187, "2017181")
   
         
    Print(AcademyFilter,"AcademyFilter",rid, 8196)
    Print(FundingBasis,"FundingBasis",rid, 8196)
    
    If FundingBasis = "Place" Then
    
    exclude(rid, 8196) 
    
    Else

       
        
       
                
           
            
           
          
            End if
    
            print(FundingBasis, "FundingBasisType",rid, 8196)
            print(AcademyFilter, "AcademyFilter", rid, 8196)
      
                If AcademyFilter = 17181 Or (AcademyFilter = 17182 And FundingBasis = "Estimate") Or (AcademyFilter = 17183 And FundingBasis = "Estimate")   then

                Dim ProviderName as string = _engine.GetDsDataValue(rid, 9070)
                Dim P048_IDACI2PriPupils As Decimal = GetProductResult_7667(rid) 
                Dim P049_IDACI2PriRate As Decimal = GetProductResult_8195(rid) 
                Dim P047_IDACI2PriFactor As Decimal = GetProductResult_8194(rid) 
                Dim P050_IDACI2PriSubtotal As Decimal = P048_IDACI2PriPupils * P049_IDACI2PriRate * P047_IDACI2PriFactor
    
                Print(ProviderName,"Provider Name",rid, 8196)
                Print(P048_IDACI2PriPupils,"P048_IDACI2PriPupils",rid, 8196)
                Print(P049_IDACI2PriRate,"P049_IDACI2PriRate",rid, 8196)
                Print(P047_IDACI2PriFactor,"P047_IDACI2PriFactor",rid, 8196)                                 

                Result = P050_IDACI2PriSubtotal  
    
                else
  
                    If (AcademyFilter = 17182 And FundingBasis = "Census") Or (AcademyFilter = 17183 And FundingBasis = "Census") then
                   
                    Dim P050_IDACI2PriSubtotalAPT As Decimal = _engine.GetDsDataValue(rid, 86106) 
                    
                    Print(P050_IDACI2PriSubtotalAPT,"P050_IDACI2PriSubtotalAPT",rid, 8196)
    
                    Result = P050_IDACI2PriSubtotalAPT
                       
                    Else
                    
                    exclude(rid, 8196)
                    
                    End If  
    
                End If  
                
       
    
        
result = System.Math.Round(result, 2, MidpointRounding.AwayFromZero)
_engine._productResultCache.Add(__key, result)
Return result
            
End Function


 Public Function GetProductResult_8199(rid As String) As Decimal
Dim __key = "8199" + rid
If (_engine._productResultCache.ContainsKey(__key)) = True Then Return _engine._productResultCache(__key)

    Dim result = 0
    
    Dim AcademyFilter As Decimal = GetProductResult_7582(rid) 
    Dim FundingBasis As String = _engine.GetDsDataValue(rid, 11187, "2017181")
     
    Print(AcademyFilter,"AcademyFilter",rid, 8199)
    Print(FundingBasis,"FundingBasis",rid, 8199)
    
    If FundingBasis = "Place" Then
    
    exclude(rid, 8199) 
    
    Else

        If AcademyFilter = 17181 Or AcademyFilter = 17182 Or AcademyFilter = 17183 then
      
        Dim P050_IDACI2PriSubtotal As Decimal = GetProductResult_8196(rid) 
        Dim Days_Open As Decimal = GetProductResult_8151(rid) 
        Dim Year_Days As Decimal = 365                     

        Print(P050_IDACI2PriSubtotal,"P050 IDACI2PriSubtotal",rid, 8199)
        Print(Days_Open,"Days Open",rid, 8199)
        Print(Year_Days,"Year Days",rid, 8199)
    
        Result = (P050_IDACI2PriSubtotal) *Divide(Days_Open, Year_Days)
    
        Else
        
        Exclude(rid, 8199) 
        
        End If  
        
    End If 
    
    
result = System.Math.Round(result, 6, MidpointRounding.AwayFromZero)
_engine._productResultCache.Add(__key, result)
Return result
    
End Function


 Public Function GetProductResult_8201(rid As String) As Decimal
Dim __key = "8201" + rid
If (_engine._productResultCache.ContainsKey(__key)) = True Then Return _engine._productResultCache(__key)

    Dim result = 0
    
    Dim AcademyFilter As Decimal = GetProductResult_7582(rid) 
    Dim FundingBasis As String = _engine.GetDsDataValue(rid, 11187, "2017181")
     
    Print(AcademyFilter,"AcademyFilter",rid, 8201)
    Print(FundingBasis,"FundingBasis",rid, 8201)
    
    If FundingBasis = "Place" Then
    
    exclude(rid, 8201) 
    
    Else

        If AcademyFilter = 17181 Or AcademyFilter = 17182 Or AcademyFilter = 17183 then
        
        Dim ProviderName As String = _engine.GetDsDataValue(rid, 9070)
        Dim P055_IDACI3PriRate As Decimal = LAtoProv(_engine.GetLaDataValue(rid, 86496, Nothing)) 
    
        Print(ProviderName,"Provider Name",rid, 8201)
        
        Result = P055_IDACI3PriRate
        
        else
        
        exclude(rid, 8201)
        
        End if
        
    End if
    
    
result = System.Math.Round(result, 6, MidpointRounding.AwayFromZero)
_engine._productResultCache.Add(__key, result)
Return result
    
End Function



 Public Function GetProductResult_8200(rid As String) As Decimal
Dim __key = "8200" + rid
If (_engine._productResultCache.ContainsKey(__key)) = True Then Return _engine._productResultCache(__key)

    Dim result = 0
    
    Dim AcademyFilter As Decimal = GetProductResult_7582(rid) 
    Dim FundingBasis As String = _engine.GetDsDataValue(rid, 11187, "2017181")
     
    Print(AcademyFilter,"AcademyFilter",rid, 8200)
    Print(FundingBasis,"FundingBasis",rid, 8200)
    
    If FundingBasis = "Place" Then
    
    exclude(rid, 8200) 
    
    Else

        If AcademyFilter = 17181 Or AcademyFilter = 17182 Or AcademyFilter = 17183 then
        
        Dim IDACI3PriCensus As Decimal = _engine.GetDsDataValue(rid, 86910)
        Dim IDACI3PriAdj As Decimal = _engine.GetDsDataValue(rid, 86026)
        Dim IDACI3PriAdjString as string = _engine.GetDsDataValue(rid, 86026)
        Dim IDACI3PriCensusString As string = _engine.GetDsDataValue(rid, 86910)
       
        Dim LA_AV As Decimal = latoprov(_engine.GetLaDataValue(rid, 86823, Nothing))
    
        Print(LA_AV,"LA average",rid, 8200)                                           
        Print(IDACI3PriCensus,"IDACI 3 Pri Census", rid, 8200)
        Print(IDACI3PriAdj,"IDACI 3 Pri Adjusted", rid, 8200) 
    
            If string.IsNullOrEmpty(IDACI3PriCensusString) And string.IsNullOrEmpty(IDACI3PriAdjString)THEN
            
            Result = LA_AV
            
            ELSE
    
                If string.IsNullOrEmpty(IDACI3PriAdjString) THEN
                
                Result = IDACI3PriCensus
                
                Else
                
                Result = IDACI3PriAdj
                
                End if
                
            End if

            else
    
            exclude(rid, 8200)
            
            End if
            
    End if
    
    
result = System.Math.Round(result, 6, MidpointRounding.AwayFromZero)
_engine._productResultCache.Add(__key, result)
Return result
    
End Function



Public Function GetProductResult_8202(rid As String) As Decimal
Dim __key = "8202" + rid
If (_engine._productResultCache.ContainsKey(__key)) = True Then Return _engine._productResultCache(__key)

    Dim result = 0
    
    Dim AcademyFilter As Decimal = GetProductResult_7582(rid) 
    Dim FundingBasis As String = _engine.GetDsDataValue(rid, 11187, "2017181")
   
     
    Print(AcademyFilter,"AcademyFilter",rid, 8202)
    Print(FundingBasis,"FundingBasis",rid, 8202)
    
        If FundingBasis = "Place" Then
        
        exclude(rid, 8202) 
        
        Else

           
            
           
                
               
                
               
          
            End if
            
            print(FundingBasis, "FundingBasisType",rid, 8202)
            print(AcademyFilter, "AcademyFilter", rid, 8202)
      
                If AcademyFilter = 17181 Or (AcademyFilter = 17182 And FundingBasis = "Estimate") Or (AcademyFilter = 17183 And FundingBasis = "Estimate")   then

                Dim ProviderName as string = _engine.GetDsDataValue(rid, 9070)
                Dim P054_IDACI3PriPupils As Decimal = GetProductResult_7667(rid) 
                Dim P055_IDACI3PriRate As Decimal = GetProductResult_8201(rid) 
                Dim P053_IDACI3PriFactor As Decimal = GetProductResult_8200(rid) 
                Dim P056_IDACI3PriSubtotal As Decimal = P054_IDACI3PriPupils * P055_IDACI3PriRate * P053_IDACI3PriFactor
    
     
                Print(ProviderName,"Provider Name",rid, 8202)
                Print(P054_IDACI3PriPupils,"P054_IDACI3PriPupils",rid, 8202)
                Print(P055_IDACI3PriRate,"P055_IDACI3PriRate",rid, 8202)
                Print(P053_IDACI3PriFactor,"P053_IDACI3PriFactor",rid, 8202)                                 

                Result = P056_IDACI3PriSubtotal 
                
                Else 
     
                    If (AcademyFilter = 17182 And FundingBasis = "Census") Or (AcademyFilter = 17183 And FundingBasis = "Census") then
                   
                    Dim P056_IDACI3PriSubtotalAPT As Decimal = _engine.GetDsDataValue(rid, 86108) 
                    
                    Print(P056_IDACI3PriSubtotalAPT,"P056_IDACI3PriSubtotalAPT",rid, 8202)
    
                    Result = P056_IDACI3PriSubtotalAPT
                       
                    Else
                    
                    exclude(rid, 8202)
                    
                    End If  
       
                End If  
       
    
    
result = System.Math.Round(result, 2, MidpointRounding.AwayFromZero)
_engine._productResultCache.Add(__key, result)
Return result
    
End Function


Public Function GetProductResult_8205(rid As String) As Decimal
Dim __key = "8205" + rid
If (_engine._productResultCache.ContainsKey(__key)) = True Then Return _engine._productResultCache(__key)

    Dim result = 0
    
    Dim AcademyFilter As Decimal = GetProductResult_7582(rid) 
    Dim FundingBasis As String = _engine.GetDsDataValue(rid, 11187, "2017181")
     
    Print(AcademyFilter,"AcademyFilter",rid, 8205)
    Print(FundingBasis,"FundingBasis",rid, 8205)
    
    If FundingBasis = "Place" Then
    
    exclude(rid, 8205)
    
    Else

        If AcademyFilter = 17181 Or AcademyFilter = 17182 Or AcademyFilter = 17183 then
      
        Dim P056_IDACI3PriSubtotal As Decimal = GetProductResult_8202(rid) 
        Dim Days_Open As Decimal = GetProductResult_8151(rid) 
        Dim Year_Days As Decimal = 365                     

        Print(P056_IDACI3PriSubtotal,"P056 IDACI3PriSubtotal",rid, 8205)
        Print(Days_Open,"Days Open",rid, 8205)
        Print(Year_Days,"Year Days",rid, 8205)
    
        Result = (P056_IDACI3PriSubtotal) *Divide(Days_Open, Year_Days)
    
        Else
        
        Exclude(rid, 8205)
        
        End If
        
    End If 
    
    
result = System.Math.Round(result, 6, MidpointRounding.AwayFromZero)
_engine._productResultCache.Add(__key, result)
Return result
    
End Function



Public Function GetProductResult_8207(rid As String) As Decimal
Dim __key = "8207" + rid
If (_engine._productResultCache.ContainsKey(__key)) = True Then Return _engine._productResultCache(__key)

    Dim result = 0
    
    Dim AcademyFilter As Decimal = GetProductResult_7582(rid) 
    Dim FundingBasis As String = _engine.GetDsDataValue(rid, 11187, "2017181")
     
    Print(AcademyFilter,"AcademyFilter",rid, 8207)
    Print(FundingBasis,"FundingBasis",rid, 8207)
    
    If FundingBasis = "Place" Then
    
    exclude(rid, 8207)
    
    Else

        If AcademyFilter = 17181 Or AcademyFilter = 17182 Or AcademyFilter = 17183 then
        
        Dim ProviderName As String = _engine.GetDsDataValue(rid, 9070)
        Dim P061_IDACI4PriRate As Decimal = LAtoProv(_engine.GetLaDataValue(rid, 86510, Nothing)) 
    
        Print(ProviderName,"Provider Name",rid, 8207)  
        
        Result = P061_IDACI4PriRate
        
        else
        
        exclude(rid, 8207)
        
        End if
        
    End if
    
    
result = System.Math.Round(result, 6, MidpointRounding.AwayFromZero)
_engine._productResultCache.Add(__key, result)
Return result
    
End Function


Public Function GetProductResult_8206(rid As String) As Decimal
Dim __key = "8206" + rid
If (_engine._productResultCache.ContainsKey(__key)) = True Then Return _engine._productResultCache(__key)

    Dim result = 0
    
    Dim AcademyFilter As Decimal = GetProductResult_7582(rid) 
    Dim FundingBasis As String = _engine.GetDsDataValue(rid, 11187, "2017181")
     
    Print(AcademyFilter,"AcademyFilter",rid, 8206)
    Print(FundingBasis,"FundingBasis",rid, 8206)
    
    If FundingBasis = "Place" Then

    exclude(rid, 8206)
    
    Else

        If AcademyFilter = 17181 Or AcademyFilter = 17182 Or AcademyFilter = 17183 then
        
        Dim IDACI4PriCensus As Decimal = _engine.GetDsDataValue(rid, 86912)
        Dim IDACI4PriAdj As Decimal = _engine.GetDsDataValue(rid, 86028)
        Dim IDACI4PriAdjString as string = _engine.GetDsDataValue(rid, 86028)
        Dim IDACI4PriCensusString As String = _engine.GetDsDataValue(rid, 86912)
        Dim LA_AV As Decimal = latoprov(_engine.GetLaDataValue(rid, 86825, Nothing))

        Print(LA_AV,"LA average",rid, 8206)
        Print(IDACI4PriCensus,"IDACI 4 Pri Census",rid, 8206)
        Print(IDACI4PriAdj,"IDACI 4 Pri Adjusted",rid, 8206)
    
            If string.IsNullOrEmpty(IDACI4PriCensusString) And string.IsNullOrEmpty(IDACI4PriAdjString) THEN
            
            Result = LA_AV
            
            ELSE
    
                If string.IsNullOrEmpty(IDACI4PriAdjString) THEN
                
                Result = IDACI4PriCensus
                
                Else
                
                Result = IDACI4PriAdj
                
                End if
                
            End if

            else
            
            exclude(rid, 8206)
            
            End if
            
    End if
    
    
result = System.Math.Round(result, 6, MidpointRounding.AwayFromZero)
_engine._productResultCache.Add(__key, result)
Return result
    
End Function




Public Function GetProductResult_8208(rid As String) As Decimal
Dim __key = "8208" + rid
If (_engine._productResultCache.ContainsKey(__key)) = True Then Return _engine._productResultCache(__key)

    Dim result = 0
    
    Dim AcademyFilter As Decimal = GetProductResult_7582(rid) 
    Dim FundingBasis As String = _engine.GetDsDataValue(rid, 11187, "2017181")
   
     
    Print(AcademyFilter,"AcademyFilter",rid, 8208)
    Print(FundingBasis,"FundingBasis",rid, 8208)
    
    If FundingBasis = "Place" Then
    
    exclude(rid, 8208) 
    
    Else
        
       
        
       
                
          
            
           
          
            End if
            
            print(FundingBasis, "FundingBasisType",rid, 8208)
            print(AcademyFilter, "AcademyFilter", rid, 8208)
      
                If AcademyFilter = 17181 Or (AcademyFilter = 17182 And FundingBasis = "Estimate") Or (AcademyFilter = 17183 And FundingBasis = "Estimate")   then
  
                Dim ProviderName as string = _engine.GetDsDataValue(rid, 9070)
                Dim P060_IDACI4PriPupils As Decimal = GetProductResult_7667(rid) 
                Dim P061_IDACI4PriRate As Decimal = GetProductResult_8207(rid) 
                Dim P059_IDACI4PriFactor As Decimal = GetProductResult_8206(rid) 
                Dim P062_IDACI4PriSubtotal As Decimal = P060_IDACI4PriPupils * P061_IDACI4PriRate * P059_IDACI4PriFactor
                                                                 
                Print(ProviderName,"Provider Name",rid, 8208)
                Print(P060_IDACI4PriPupils,"P060_IDACI4PriPupils",rid, 8208)
                Print(P061_IDACI4PriRate,"P061_IDACI4PriRate",rid, 8208)
                Print(P059_IDACI4PriFactor,"P059_IDACI4PriFactor",rid, 8208)                                 

                Result = P062_IDACI4PriSubtotal  
                
                else
     
                    If (AcademyFilter = 17182 And FundingBasis = "Census") Or (AcademyFilter = 17183 And FundingBasis = "Census") then
                   
                    Dim P062_IDACI4PriSubtotalAPT As Decimal = _engine.GetDsDataValue(rid, 86110) 
                    
                    Print(P062_IDACI4PriSubtotalAPT,"P062_IDACI4PriSubtotalAPT",rid, 8208)
    
                    Result = P062_IDACI4PriSubtotalAPT
                       
                    Else
                    
                    exclude(rid, 8208)
                    
                    End If  
          
            End If  
   
    
    
result = System.Math.Round(result, 2, MidpointRounding.AwayFromZero)
_engine._productResultCache.Add(__key, result)
Return result
    
End Function


Public Function GetProductResult_8211(rid As String) As Decimal
Dim __key = "8211" + rid
If (_engine._productResultCache.ContainsKey(__key)) = True Then Return _engine._productResultCache(__key)

    Dim result = 0
    
    Dim AcademyFilter As Decimal = GetProductResult_7582(rid) 
    Dim FundingBasis As String = _engine.GetDsDataValue(rid, 11187, "2017181")
     
    Print(AcademyFilter,"AcademyFilter",rid, 8211)
    Print(FundingBasis,"FundingBasis",rid, 8211)
    
    If FundingBasis = "Place" Then
    
    exclude(rid, 8211) 
    
    Else

        If AcademyFilter = 17181 Or AcademyFilter = 17182 Or AcademyFilter = 17183 then
      
        Dim P062_IDACI4PriSubtotal As Decimal = GetProductResult_8208(rid) 
        Dim Days_Open As Decimal = GetProductResult_8151(rid) 
        Dim Year_Days As Decimal = 365                    

        Print(P062_IDACI4PriSubtotal,"P062 IDACI4PriSubtotal",rid, 8211)
        Print(Days_Open,"Days Open",rid, 8211)
        Print(Year_Days,"Year Days",rid, 8211)
    
        Result = (P062_IDACI4PriSubtotal) *Divide(Days_Open, Year_Days)
    
        Else
        
        Exclude(rid, 8211)
        
        End If  
        
    End if
    
    
result = System.Math.Round(result, 6, MidpointRounding.AwayFromZero)
_engine._productResultCache.Add(__key, result)
Return result
    
End Function




 Public Function GetProductResult_8213(rid As String) As Decimal
Dim __key = "8213" + rid
If (_engine._productResultCache.ContainsKey(__key)) = True Then Return _engine._productResultCache(__key)

    Dim result = 0
    
    Dim AcademyFilter As Decimal = GetProductResult_7582(rid) 
    Dim FundingBasis As String = _engine.GetDsDataValue(rid, 11187, "2017181")
     
    Print(AcademyFilter,"AcademyFilter",rid, 8213)
    Print(FundingBasis,"FundingBasis",rid, 8213)
    
    If FundingBasis = "Place" Then
    
    exclude(rid, 8213)
    
    Else

        If AcademyFilter = 17181 Or AcademyFilter = 17182 Or AcademyFilter = 17183 then
        
        Dim ProviderName As String = _engine.GetDsDataValue(rid, 9070)
        Dim P067_IDACI5PriRate As Decimal = LAtoProv(_engine.GetLaDataValue(rid, 86524, Nothing)) 
    
        Print(ProviderName,"Provider Name",rid, 8213) 
        
        Result = P067_IDACI5PriRate
        
        else
        
        exclude(rid, 8213)
        
        End if
        
    End if
    
    
result = System.Math.Round(result, 6, MidpointRounding.AwayFromZero)
_engine._productResultCache.Add(__key, result)
Return result
    
End Function


Public Function GetProductResult_8212(rid As String) As Decimal
Dim __key = "8212" + rid
If (_engine._productResultCache.ContainsKey(__key)) = True Then Return _engine._productResultCache(__key)

    Dim result = 0
    
    Dim AcademyFilter As Decimal = GetProductResult_7582(rid) 
    Dim FundingBasis As String = _engine.GetDsDataValue(rid, 11187, "2017181")
     
    Print(AcademyFilter,"AcademyFilter",rid, 8212)
    Print(FundingBasis,"FundingBasis",rid, 8212)
    
    If FundingBasis = "Place" Then
    
    exclude(rid, 8212)
    
    Else

        If AcademyFilter = 17181 Or AcademyFilter = 17182 Or AcademyFilter = 17183 then
     
        Dim IDACI5PriCensus As Decimal = _engine.GetDsDataValue(rid, 86914)
        Dim IDACI5PriAdj As Decimal = _engine.GetDsDataValue(rid, 86030)
        Dim IDACI5PriAdjString as string = _engine.GetDsDataValue(rid, 86030)
        Dim IDACI5PriCensusString As String = _engine.GetDsDataValue(rid, 86914)
        Dim LA_AV As Decimal = LAtoProv(_engine.GetLaDataValue(rid, 86827, Nothing))    
    
        Print(LA_AV,"LA average",rid, 8212)
        Print(IDACI5PriCensus,"IDACI 5 Pri Census", rid, 8212)
        Print(IDACI5PriAdj,"IDACI 5 Pri Adjusted", rid, 8212)

            If string.IsNullOrEmpty(IDACI5PriCensusString) And string.IsNullOrEmpty(IDACI5PriAdjString) THEN
        
            Result = LA_AV
            
            ELSE

                If string.IsNullOrEmpty(IDACI5PriAdjString) THEN
                
                Result = IDACI5PriCensus
                
                Else
                
                Result = IDACI5PriAdj
                
                End if
            End if
    
            else
            
            exclude(rid, 8212)
            
        End if
        
    End if
    
    
result = System.Math.Round(result, 6, MidpointRounding.AwayFromZero)
_engine._productResultCache.Add(__key, result)
Return result
    
End Function


Public Function GetProductResult_8214(rid As String) As Decimal
Dim __key = "8214" + rid
If (_engine._productResultCache.ContainsKey(__key)) = True Then Return _engine._productResultCache(__key)

    Dim result = 0
    
    Dim AcademyFilter As Decimal = GetProductResult_7582(rid) 
    Dim FundingBasis As String = _engine.GetDsDataValue(rid, 11187, "2017181")
   
     
    Print(AcademyFilter,"AcademyFilter",rid, 8214)
    Print(FundingBasis,"FundingBasis",rid, 8214)
    
    If FundingBasis = "Place" Then
    
    exclude(rid, 8214)
    
    Else
        
       
        
      
                
           
            
           
          
            End if
            
            print(FundingBasis, "FundingBasisType",rid, 8214)
            print(AcademyFilter, "AcademyFilter", rid, 8214)
      
                If AcademyFilter = 17181 Or (AcademyFilter = 17182 And FundingBasis = "Estimate") Or (AcademyFilter = 17183 And FundingBasis = "Estimate")   then

                Dim ProviderName as string = _engine.GetDsDataValue(rid, 9070)
                Dim P066_IDACI5PriPupils As Decimal = GetProductResult_7667(rid) 
                Dim P067_IDACI5PriRate As Decimal = GetProductResult_8213(rid) 
                Dim P065_IDACI5PriFactor As Decimal = GetProductResult_8212(rid) 
                Dim P068_IDACI5PriSubtotal As Decimal = P066_IDACI5PriPupils * P067_IDACI5PriRate * P065_IDACI5PriFactor
                                                               
                Print(ProviderName,"Provider Name",rid, 8214)
                Print(P066_IDACI5PriPupils,"P066_IDACI5PriPupils",rid, 8214)
                Print(P067_IDACI5PriRate,"P067_IDACI5PriRate",rid, 8214)
                Print(P065_IDACI5PriFactor,"P065_IDACI5PriFactor",rid, 8214)                                 

                Result = P068_IDACI5PriSubtotal
                
                else
      
                    If (AcademyFilter = 17182 And FundingBasis = "Census") Or (AcademyFilter = 17183 And FundingBasis = "Census") then
                   
                    Dim P068_IDACI5PriSubtotalAPT As Decimal = _engine.GetDsDataValue(rid, 86112) 
                    
                    Print(P068_IDACI5PriSubtotalAPT,"P068_IDACI5PriSubtotalAPT",rid, 8214)
    
                    Result = P068_IDACI5PriSubtotalAPT
                       
                    Else 
                    
                    exclude(rid, 8214)
                    
                    End If  
      
                End If
                
           
    
    
result = System.Math.Round(result, 2, MidpointRounding.AwayFromZero)
_engine._productResultCache.Add(__key, result)
Return result
    
End Function


Public Function GetProductResult_8217(rid As String) As Decimal
Dim __key = "8217" + rid
If (_engine._productResultCache.ContainsKey(__key)) = True Then Return _engine._productResultCache(__key)

    Dim result = 0
    
    Dim AcademyFilter As Decimal = GetProductResult_7582(rid) 
    Dim FundingBasis As String = _engine.GetDsDataValue(rid, 11187, "2017181")
     
    Print(AcademyFilter,"AcademyFilter",rid, 8217)
    Print(FundingBasis,"FundingBasis",rid, 8217)
    
    If FundingBasis = "Place" Then
    
    exclude(rid, 8217)
    
    Else
    
        If AcademyFilter = 17181 Or AcademyFilter = 17182 Or AcademyFilter = 17183 then
      
        Dim P068_IDACI5PriSubtotal As Decimal = GetProductResult_8214(rid) 
        Dim Days_Open As Decimal = GetProductResult_8151(rid) 
        Dim Year_Days As Decimal = 365                     

        Print(P068_IDACI5PriSubtotal,"P068 IDACI5PriSubtotal",rid, 8217)
        Print(Days_Open,"Days Open",rid, 8217)
        Print(Year_Days,"Year Days",rid, 8217)
    
        Result = (P068_IDACI5PriSubtotal) *Divide(Days_Open, Year_Days)
    
        Else
        
        Exclude(rid, 8217)
        
        End If  
        
    End if
    
    
result = System.Math.Round(result, 6, MidpointRounding.AwayFromZero)
_engine._productResultCache.Add(__key, result)
Return result
    
End Function



Public Function GetProductResult_8219(rid As String) As Decimal
Dim __key = "8219" + rid
If (_engine._productResultCache.ContainsKey(__key)) = True Then Return _engine._productResultCache(__key)

    Dim result = 0
    
    Dim AcademyFilter As Decimal = GetProductResult_7582(rid) 
    Dim FundingBasis As String = _engine.GetDsDataValue(rid, 11187, "2017181")
     
    Print(AcademyFilter,"AcademyFilter",rid, 8219)
    Print(FundingBasis,"FundingBasis",rid, 8219)
    
    If FundingBasis = "Place" Then
    
    exclude(rid, 8219)
    
    Else

        If AcademyFilter = 17181 Or AcademyFilter = 17182 Or AcademyFilter = 17183 then
        
        Dim ProviderName As String = _engine.GetDsDataValue(rid, 9070)
        Dim P073_IDACI6PriRate As Decimal = LAtoProv(_engine.GetLaDataValue(rid, 86538, Nothing)) 
           
        Result = P073_IDACI6PriRate 
        
        else
        
        exclude(rid, 8219)
        
        End if
        
     End if
     
    
result = System.Math.Round(result, 6, MidpointRounding.AwayFromZero)
_engine._productResultCache.Add(__key, result)
Return result
    
End Function


Public Function GetProductResult_8218(rid As String) As Decimal
Dim __key = "8218" + rid
If (_engine._productResultCache.ContainsKey(__key)) = True Then Return _engine._productResultCache(__key)

    Dim result = 0
    
    Dim AcademyFilter As Decimal = GetProductResult_7582(rid) 
    Dim FundingBasis As String = _engine.GetDsDataValue(rid, 11187, "2017181")
     
    Print(AcademyFilter,"AcademyFilter",rid, 8218)
    Print(FundingBasis,"FundingBasis",rid, 8218)
    
    If FundingBasis = "Place" Then
    
    exclude(rid, 8218)
    
    Else

        If AcademyFilter = 17181 Or AcademyFilter = 17182 Or AcademyFilter = 17183 then
    
        Dim IDACI6PriCensus As Decimal = _engine.GetDsDataValue(rid, 86916)
        Dim IDACI6PriAdj As Decimal = _engine.GetDsDataValue(rid, 86032)
        Dim IDACI6PriAdjString as string = _engine.GetDsDataValue(rid, 86032)
        Dim IDACI6PriCensusString As String = _engine.GetDsDataValue(rid, 86916)
        Dim LA_AV As Decimal = LAtoProv(_engine.GetLaDataValue(rid, 86829, Nothing))
    
        Print(LA_AV,"LA average",rid, 8218)
        Print(IDACI6PriCensus,"IDACI 6 Pri Census",rid, 8218)
        Print(IDACI6PriAdj,"IDACI 6 Pri Adjusted",rid, 8218)
    
        If string.IsNullOrEmpty(IDACI6PriCensusString) And string.IsNullOrEmpty(IDACI6PriAdjString) THEN
        
        Result = LA_AV
        
        ELSE
    
            If string.IsNullOrEmpty(IDACI6PriAdjString) THEN
            
            Result = IDACI6PriCensus
            
            Else
            
            Result = IDACI6PriAdj
            
            End if
            
        End if
    
        else
    
        exclude(rid, 8218)
        
        End if
        
    End if
    
    
result = System.Math.Round(result, 6, MidpointRounding.AwayFromZero)
_engine._productResultCache.Add(__key, result)
Return result
    
End Function


Public Function GetProductResult_8220(rid As String) As Decimal
Dim __key = "8220" + rid
If (_engine._productResultCache.ContainsKey(__key)) = True Then Return _engine._productResultCache(__key)

    Dim result = 0
    
    Dim AcademyFilter As Decimal = GetProductResult_7582(rid) 
    Dim FundingBasis As String = _engine.GetDsDataValue(rid, 11187, "2017181")
   
     
    Print(AcademyFilter,"AcademyFilter",rid, 8220)
    Print(FundingBasis,"FundingBasis",rid, 8220)
    
    If FundingBasis = "Place" Then
    
    exclude(rid, 8220)
    
    Else
        
       
        
       
                
       
        
       
          
        End if
        
        print(FundingBasis, "FundingBasisType",rid, 8220)
        print(AcademyFilter, "AcademyFilter", rid, 8220)
      
            If AcademyFilter = 17181 Or (AcademyFilter = 17182 And FundingBasis = "Estimate") Or (AcademyFilter = 17183 And FundingBasis = "Estimate")   then
      
            Dim P072_IDACI6PriPupils As Decimal = GetProductResult_7667(rid) 
            Dim P073_IDACI6PriRate As Decimal = GetProductResult_8219(rid) 
            Dim P071_IDACI6PriFactor As Decimal = GetProductResult_8218(rid) 
            Dim P074_IDACI6PriSubtotal As Decimal = P072_IDACI6PriPupils * P073_IDACI6PriRate * P071_IDACI6PriFactor
   
            Print(P072_IDACI6PriPupils,"P072_IDACI6PriPupils",rid, 8220)
            Print(P073_IDACI6PriRate,"P073_IDACI6PriRate",rid, 8220)
            Print(P071_IDACI6PriFactor,"P071_IDACI6PriFactor",rid, 8220)                                 

            Result = P074_IDACI6PriSubtotal  
            
            else
  
                If (AcademyFilter = 17182 And FundingBasis = "Census") Or (AcademyFilter = 17183 And FundingBasis = "Census") then
                   
                Dim P074_IDACI6PriSubtotalAPT As Decimal = _engine.GetDsDataValue(rid, 86114)
                
                Print(P074_IDACI6PriSubtotalAPT,"P074_IDACI6PriSubtotalAPT",rid, 8220)
    
                Result = P074_IDACI6PriSubtotalAPT
                       
                Else
                
                exclude(rid, 8220)
                
                End If  
        
            End If  
    
   
    
    
result = System.Math.Round(result, 2, MidpointRounding.AwayFromZero)
_engine._productResultCache.Add(__key, result)
Return result
    
End Function


Public Function GetProductResult_8223(rid As String) As Decimal
Dim __key = "8223" + rid
If (_engine._productResultCache.ContainsKey(__key)) = True Then Return _engine._productResultCache(__key)

    Dim result = 0
    
    Dim AcademyFilter As Decimal = GetProductResult_7582(rid) 
    Dim FundingBasis As String = _engine.GetDsDataValue(rid, 11187, "2017181")
     
    Print(AcademyFilter,"AcademyFilter",rid, 8223)
    Print(FundingBasis,"FundingBasis",rid, 8223)
    
    If FundingBasis = "Place" Then
    
    exclude(rid, 8223)
    
    Else

        If AcademyFilter = 17181 Or AcademyFilter = 17182 Or AcademyFilter = 17183 then
      
        Dim P074_IDACI6PriSubtotal As Decimal = GetProductResult_8220(rid) 
        Dim Days_Open As Decimal = GetProductResult_8151(rid) 
        Dim Year_Days As Decimal = 365                    

        Print(P074_IDACI6PriSubtotal,"P074 IDACI6PriSubtotal",rid, 8223)
        Print(Days_Open,"Days Open",rid, 8223)
        Print(Year_Days,"Year Days",rid, 8223)
    
        Result = (P074_IDACI6PriSubtotal) *Divide(Days_Open, Year_Days)
    
        Else
        
        Exclude(rid, 8223)  
        
        End If 
        
    End if
    
    
result = System.Math.Round(result, 6, MidpointRounding.AwayFromZero)
_engine._productResultCache.Add(__key, result)
Return result
    
End Function



 Public Function GetProductResult_8225(rid As String) As Decimal
Dim __key = "8225" + rid
If (_engine._productResultCache.ContainsKey(__key)) = True Then Return _engine._productResultCache(__key)

    Dim result = 0
    
    Dim AcademyFilter As Decimal = GetProductResult_7582(rid) 
    Dim FundingBasis As String = _engine.GetDsDataValue(rid, 11187, "2017181")
     
    Print(AcademyFilter,"AcademyFilter",rid, 8225)
    Print(FundingBasis,"FundingBasis",rid, 8225)
    
    If FundingBasis = "Place" Then
    
    exclude(rid, 8225) 
    
    Else
    
        If AcademyFilter = 17181 Or AcademyFilter = 17182 Or AcademyFilter = 17183 then
        
        Dim ProviderName as string = _engine.GetDsDataValue(rid, 9070)
        Dim P079_IDACI1SecRate As Decimal = LAtoProv(_engine.GetLaDataValue(rid, 86470, Nothing)) 
           
        Result = P079_IDACI1SecRate
        
        else
        
        exclude(rid, 8225)
        
        End if
        
    End if
    
    
result = System.Math.Round(result, 6, MidpointRounding.AwayFromZero)
_engine._productResultCache.Add(__key, result)
Return result
    
End Function


 Public Function GetProductResult_8224(rid As String) As Decimal
Dim __key = "8224" + rid
If (_engine._productResultCache.ContainsKey(__key)) = True Then Return _engine._productResultCache(__key)

    Dim result = 0
    
    Dim AcademyFilter As Decimal = GetProductResult_7582(rid) 
    Dim FundingBasis As String = _engine.GetDsDataValue(rid, 11187, "2017181")
     
    Print(AcademyFilter,"AcademyFilter",rid, 8224)
    Print(FundingBasis,"FundingBasis",rid, 8224)
    
    If FundingBasis = "Place" Then
    
    exclude(rid, 8224) 
    
    Else

        If AcademyFilter = 17181 Or AcademyFilter = 17182 Or AcademyFilter = 17183 then
        
        Dim IDACI1SecCensus As Decimal = _engine.GetDsDataValue(rid, 86920)
        Dim IDACI1SecAdj As Decimal = _engine.GetDsDataValue(rid, 86036)
        Dim IDACI1SecAdjString as string = _engine.GetDsDataValue(rid, 86036)
        Dim IDACI1SecCensusString as string  = _engine.GetDsDataValue(rid, 86920)
        Dim LA_AV As Decimal = latoprov(_engine.GetLaDataValue(rid, 86833, Nothing))

        Print(LA_AV,"LA average",rid, 8224)
        Print(IDACI1SecCensus,"IDACI 1 Sec Census",rid, 8224)
        Print(IDACI1SecAdj,"IDACI 1 Sec Adjusted",rid, 8224)
    
            If string.IsNullOrEmpty(IDACI1SecCensusString) And string.IsNullOrEmpty(IDACI1SecAdjString)THEN
        
            Result = LA_AV
        
            ELSE
    
                If string.IsNullOrEmpty(IDACI1SecAdjString) THEN
            
                Result = IDACI1SecCensus
            
                Else
            
                Result = IDACI1SecAdj
            
                End if
            
            End if
    
            else
    
            exclude(rid, 8224)
        
            End if
            
    End if
       
    
result = System.Math.Round(result, 6, MidpointRounding.AwayFromZero)
_engine._productResultCache.Add(__key, result)
Return result
    
End Function



 Public Function GetProductResult_8226(rid As String) As Decimal
Dim __key = "8226" + rid
If (_engine._productResultCache.ContainsKey(__key)) = True Then Return _engine._productResultCache(__key)

    Dim result = 0
    
    Dim AcademyFilter As Decimal = GetProductResult_7582(rid) 
    Dim FundingBasis As String = _engine.GetDsDataValue(rid, 11187, "2017181")
   
         
    Print(AcademyFilter,"AcademyFilter",rid, 8226)
    Print(FundingBasis,"FundingBasis",rid, 8226)
    
    If FundingBasis = "Place" Then
    
    exclude(rid, 8226) 
    
    Else
        
       
        
       
                
           
            
           
          
            End if
            
            print(FundingBasis, "FundingBasisType",rid, 8226)
            print(AcademyFilter, "AcademyFilter", rid, 8226)
      
                If AcademyFilter = 17181 Or (AcademyFilter = 17182 And FundingBasis = "Estimate") Or (AcademyFilter = 17183 And FundingBasis = "Estimate")   then
       
                Dim P078_IDACI1SecPupils As Decimal = GetProductResult_7673(rid) 
                Dim P079_IDACI1SecRate As Decimal = GetProductResult_8225(rid) 
                Dim P077_IDACI1SecFactor As Decimal = GetProductResult_8224(rid) 
                Dim P080_IDACI1SecSubtotal As Decimal = P078_IDACI1SecPupils * P079_IDACI1SecRate * P077_IDACI1SecFactor
    
                Print(P078_IDACI1SecPupils,"P078_IDACI1SecPupils",rid, 8226)
                Print(P079_IDACI1SecRate,"P079_IDACI1SecRate",rid, 8226)
                Print(P077_IDACI1SecFactor,"P077_IDACI1SecFactor",rid, 8226)                                 

                Result = P080_IDACI1SecSubtotal  
    
                else
    
                    If (AcademyFilter = 17182 And FundingBasis = "Census") Or (AcademyFilter = 17183 And FundingBasis = "Census") then
                   
                    Dim P080_IDACI1SecSubtotalAPT As Decimal = _engine.GetDsDataValue(rid, 86118)
                    
                    Print(P080_IDACI1SecSubtotalAPT,"P080_IDACI1SecSubtotalAPT",rid, 8226)
    
                    Result = P080_IDACI1SecSubtotalAPT
                       
                    Else  
                    
                    exclude(rid, 8226)
                    
                    End If  
         
                End If  
                
           
    

result = System.Math.Round(result, 2, MidpointRounding.AwayFromZero)
_engine._productResultCache.Add(__key, result)
Return result

End Function


Public Function GetProductResult_8229(rid As String) As Decimal
Dim __key = "8229" + rid
If (_engine._productResultCache.ContainsKey(__key)) = True Then Return _engine._productResultCache(__key)

    Dim result = 0
    
    Dim AcademyFilter As Decimal = GetProductResult_7582(rid) 
    Dim FundingBasis As String = _engine.GetDsDataValue(rid, 11187, "2017181")
     
    Print(AcademyFilter,"AcademyFilter",rid, 8229)
    Print(FundingBasis,"FundingBasis",rid, 8229)
    
    If FundingBasis = "Place" Then
    
    exclude(rid, 8229)
    
    Else

        If AcademyFilter = 17181 Or AcademyFilter = 17182 Or AcademyFilter = 17183 then
      
        Dim P080_IDACI1SecSubtotal As Decimal = GetProductResult_8226(rid) 
        Dim Days_Open As Decimal = GetProductResult_8151(rid) 
        Dim Year_Days As Decimal = 365                     

        Print(P080_IDACI1SecSubtotal,"P080 IDACI1SecSubtotal",rid, 8229)
        Print(Days_Open,"Days Open",rid, 8229)
        Print(Year_Days,"Year Days",rid, 8229)
    
        Result = (P080_IDACI1SecSubtotal) *Divide(Days_Open, Year_Days)
    
        Else
        
        Exclude(rid, 8229) 
        
        End If 
        
    End if
    
    
result = System.Math.Round(result, 6, MidpointRounding.AwayFromZero)
_engine._productResultCache.Add(__key, result)
Return result
    
End Function


 Public Function GetProductResult_8231(rid As String) As Decimal
Dim __key = "8231" + rid
If (_engine._productResultCache.ContainsKey(__key)) = True Then Return _engine._productResultCache(__key)

    Dim result = 0
    
    Dim AcademyFilter As Decimal = GetProductResult_7582(rid) 
    Dim FundingBasis As String = _engine.GetDsDataValue(rid, 11187, "2017181")
     
    Print(AcademyFilter,"AcademyFilter",rid, 8231)
    Print(FundingBasis,"FundingBasis",rid, 8231)
    
    If FundingBasis = "Place" Then
    
    exclude(rid, 8231) 
    
    Else

        If AcademyFilter = 17181 Or AcademyFilter = 17182 Or AcademyFilter = 17183 then
        
        Dim ProviderName as string = _engine.GetDsDataValue(rid, 9070)
        Dim P085_IDACI2SecRate As Decimal = LAtoProv(_engine.GetLaDataValue(rid, 86484, Nothing)) 
          
        Result = P085_IDACI2SecRate
        
        else
        
        exclude(rid, 8231)
        
        End if
        
    End if
    
    
result = System.Math.Round(result, 6, MidpointRounding.AwayFromZero)
_engine._productResultCache.Add(__key, result)
Return result
    
End Function


Public Function GetProductResult_8230(rid As String) As Decimal
Dim __key = "8230" + rid
If (_engine._productResultCache.ContainsKey(__key)) = True Then Return _engine._productResultCache(__key)

    Dim result = 0
    
    Dim AcademyFilter As Decimal = GetProductResult_7582(rid) 
    Dim FundingBasis As String = _engine.GetDsDataValue(rid, 11187, "2017181")
     
    Print(AcademyFilter,"AcademyFilter",rid, 8230)
    Print(FundingBasis,"FundingBasis",rid, 8230)
    
    If FundingBasis = "Place" Then
    
    exclude(rid, 8230) 
    
    Else

        If AcademyFilter = 17181 Or AcademyFilter = 17182 Or AcademyFilter = 17183 then
       
        Dim IDACI2SecCensus As Decimal = _engine.GetDsDataValue(rid, 86922)
        Dim IDACI2SecAdj As Decimal = _engine.GetDsDataValue(rid, 86038)
        Dim IDACI2SecAdjString as string = _engine.GetDsDataValue(rid, 86038)
        Dim IDACI2SecCensusString as string  = _engine.GetDsDataValue(rid, 86922)
        Dim LA_AV As Decimal = latoprov(_engine.GetLaDataValue(rid, 86835, Nothing))

        Print(LA_AV,"LA average",rid, 8230)
        Print(IDACI2SecCensus,"IDACI 2 Sec Census",rid, 8230)
        Print(IDACI2SecAdj,"IDACI 2 Sec Adjusted",rid, 8230)

            If string.IsNullOrEmpty(IDACI2SecCensusString) And string.IsNullOrEmpty(IDACI2SecAdjString) THEN
            
            Result = LA_AV
            
            ELSE
    
                If string.IsNullOrEmpty(IDACI2SecAdjString) THEN
                
                Result = IDACI2SecCensus
                
                Else
                
                Result = IDACI2SecAdj
                
                End if
                
            End if
    
            else
            
            exclude(rid, 8230)
            
        End if
            
    End if
    
    
result = System.Math.Round(result, 6, MidpointRounding.AwayFromZero)
_engine._productResultCache.Add(__key, result)
Return result
    
End Function


Public Function GetProductResult_8232(rid As String) As Decimal
Dim __key = "8232" + rid
If (_engine._productResultCache.ContainsKey(__key)) = True Then Return _engine._productResultCache(__key)

    Dim result = 0
    
    Dim AcademyFilter As Decimal = GetProductResult_7582(rid) 
    Dim FundingBasis As String = _engine.GetDsDataValue(rid, 11187, "2017181")
   
     
    Print(AcademyFilter,"AcademyFilter",rid, 8232)
    Print(FundingBasis,"FundingBasis",rid, 8232)
    
    If FundingBasis = "Place" Then
    
    exclude(rid, 8232) 
    
    Else

       
        
       
                
       
         
       
          
        End if
        
        print(FundingBasis, "FundingBasisType",rid, 8232)
        print(AcademyFilter, "AcademyFilter", rid, 8232)
      
            If AcademyFilter = 17181 Or (AcademyFilter = 17182 And FundingBasis = "Estimate") Or (AcademyFilter = 17183 And FundingBasis = "Estimate")   then
        
            Dim P084_IDACI2SecPupils As Decimal = GetProductResult_7673(rid) 
            Dim P085_IDACI2SecRate As Decimal = GetProductResult_8231(rid) 
            Dim P083_IDACI2SecFactor As Decimal = GetProductResult_8230(rid) 
            Dim P086_IDACI2SecSubtotal As Decimal = P084_IDACI2SecPupils * P085_IDACI2SecRate * P083_IDACI2SecFactor
    
            Print(P084_IDACI2SecPupils,"P084_IDACI2SecPupils",rid, 8232)
            Print(P085_IDACI2SecRate,"P085_IDACI2SecRate",rid, 8232)
            Print(P083_IDACI2SecFactor,"P083_IDACI2SecFactor",rid, 8232)                                 

            Result = P086_IDACI2SecSubtotal  
   
            else
    
                If (AcademyFilter = 17182 And FundingBasis = "Census") Or (AcademyFilter = 17183 And FundingBasis = "Census") then
                   
                    Dim P086_IDACI2SecSubtotalAPT As Decimal = _engine.GetDsDataValue(rid, 86120)
                    
                    Print(P086_IDACI2SecSubtotalAPT,"P086_IDACI2SecSubtotalAPT",rid, 8232)
    
                    Result = P086_IDACI2SecSubtotalAPT
                       
                    Else
                    
                    exclude(rid, 8232)
                    
                    End If  
      
                End If 
                
           
    
    
result = System.Math.Round(result, 2, MidpointRounding.AwayFromZero)
_engine._productResultCache.Add(__key, result)
Return result
    
End Function


Public Function GetProductResult_8235(rid As String) As Decimal
Dim __key = "8235" + rid
If (_engine._productResultCache.ContainsKey(__key)) = True Then Return _engine._productResultCache(__key)

    Dim result = 0
    
    Dim AcademyFilter As Decimal = GetProductResult_7582(rid) 
    Dim FundingBasis As String = _engine.GetDsDataValue(rid, 11187, "2017181")
     
    Print(AcademyFilter,"AcademyFilter",rid, 8235)
    Print(FundingBasis,"FundingBasis",rid, 8235)
    
    If FundingBasis = "Place" Then
    
    exclude(rid, 8235) 
    
    Else

        If AcademyFilter = 17181 Or AcademyFilter = 17182 Or AcademyFilter = 17183 then
      
        Dim P086_IDACI2SecSubtotal As Decimal = GetProductResult_8232(rid) 
        Dim Days_Open As Decimal = GetProductResult_8151(rid) 
        Dim Year_Days As Decimal = 365                    

        Print(P086_IDACI2SecSubtotal,"P086 IDACI2SecSubtotal",rid, 8235)
        Print(Days_Open,"Days Open",rid, 8235)
        Print(Year_Days,"Year Days",rid, 8235)
    
        Result = (P086_IDACI2SecSubtotal) *Divide(Days_Open, Year_Days)
    
        Else
        
        Exclude(rid, 8235) 
        
        End If   
        
    End if
    
    
result = System.Math.Round(result, 6, MidpointRounding.AwayFromZero)
_engine._productResultCache.Add(__key, result)
Return result
    
End Function


Public Function GetProductResult_8237(rid As String) As Decimal
Dim __key = "8237" + rid
If (_engine._productResultCache.ContainsKey(__key)) = True Then Return _engine._productResultCache(__key)

    Dim result = 0
    
    Dim AcademyFilter As Decimal = GetProductResult_7582(rid) 
    Dim FundingBasis As String = _engine.GetDsDataValue(rid, 11187, "2017181")
     
    Print(AcademyFilter,"AcademyFilter",rid, 8237)
    Print(FundingBasis,"FundingBasis",rid, 8237)
    
    If FundingBasis = "Place" Then
    
    exclude(rid, 8237)
    
    Else
    
        If AcademyFilter = 17181 Or AcademyFilter = 17182 Or AcademyFilter = 17183 then
        
        Dim ProviderName as string = _engine.GetDsDataValue(rid, 9070)
        Dim P091_IDACI3SecRate As Decimal = LAtoProv(_engine.GetLaDataValue(rid, 86498, Nothing)) 
    
        Result = P091_IDACI3SecRate
        
        else
        
        exclude(rid, 8237)
        
        End if
        
    End if
    
    
result = System.Math.Round(result, 6, MidpointRounding.AwayFromZero)
_engine._productResultCache.Add(__key, result)
Return result
    
End Function


Public Function GetProductResult_8236(rid As String) As Decimal
Dim __key = "8236" + rid
If (_engine._productResultCache.ContainsKey(__key)) = True Then Return _engine._productResultCache(__key)

    Dim result = 0
    
    Dim AcademyFilter As Decimal = GetProductResult_7582(rid) 
    Dim FundingBasis As String = _engine.GetDsDataValue(rid, 11187, "2017181")
     
    Print(AcademyFilter,"AcademyFilter",rid, 8236)
    Print(FundingBasis,"FundingBasis",rid, 8236)
    
    If FundingBasis = "Place" Then
    
    exclude(rid, 8236) 
    
    Else

        If AcademyFilter = 17181 Or AcademyFilter = 17182 Or AcademyFilter = 17183 then
        
        Dim IDACI3SecCensus As Decimal = _engine.GetDsDataValue(rid, 86924)
        Dim IDACI3SecAdj As Decimal = _engine.GetDsDataValue(rid, 86040)
        Dim IDACI3SecAdjString as string = _engine.GetDsDataValue(rid, 86040)
        Dim IDACI3SecCensusString as string  = _engine.GetDsDataValue(rid, 86924)
        Dim LA_AV As Decimal = latoprov(_engine.GetLaDataValue(rid, 86837, Nothing))

        Print(LA_AV,"LA average",rid, 8236) 
        Print(IDACI3SecCensus,"IDACI 3 Sec Census", rid, 8236)
        Print(IDACI3SecAdj,"IDACI 3 Sec Adjusted", rid, 8236) 

            If string.IsNullOrEmpty(IDACI3SecCensusString) And  string.IsNullOrEmpty(IDACI3SecAdjString) Then
            
            Result = LA_AV
            
            Else
    
                If string.IsNullOrEmpty(IDACI3SecAdjString) THEN
                
                Result = IDACI3SecCensus
                
                Else
                
                Result = IDACI3SecAdj
                
                End if
                
            End if
        
            else
            
            exclude(rid, 8236)
            
         End if
         
    End if
     
    
result = System.Math.Round(result, 6, MidpointRounding.AwayFromZero)
_engine._productResultCache.Add(__key, result)
Return result
    
End Function



 Public Function GetProductResult_8238(rid As String) As Decimal
Dim __key = "8238" + rid
If (_engine._productResultCache.ContainsKey(__key)) = True Then Return _engine._productResultCache(__key)

    Dim result = 0
    
    Dim AcademyFilter As Decimal = GetProductResult_7582(rid) 
    Dim FundingBasis As String = _engine.GetDsDataValue(rid, 11187, "2017181")
   
     
    Print(AcademyFilter,"AcademyFilter",rid, 8238)
    Print(FundingBasis,"FundingBasis",rid, 8238)
    
    If FundingBasis = "Place" Then
    
    exclude(rid, 8238) 
    
    Else

       
        
       
                
           
            
           
          
            End if
            
            print(FundingBasis, "FundingBasisType",rid, 8238)
            print(AcademyFilter, "AcademyFilter", rid, 8238)
      
                If AcademyFilter = 17181 Or (AcademyFilter = 17182 And FundingBasis = "Estimate") Or (AcademyFilter = 17183 And FundingBasis = "Estimate")   then 
                
                Dim P090_IDACI3SecPupils As Decimal = GetProductResult_7673(rid) 
                Dim P091_IDACI3SecRate As Decimal = GetProductResult_8237(rid) 
                Dim P089_IDACI3SecFactor As Decimal = GetProductResult_8236(rid) 
                Dim P092_IDACI3SecSubtotal As Decimal = P090_IDACI3SecPupils * P091_IDACI3SecRate * P089_IDACI3SecFactor
     
                Print(P090_IDACI3SecPupils,"P090_IDACI3SecPupils",rid, 8238)
                Print(P091_IDACI3SecRate,"P091_IDACI3SecRate",rid, 8238)
                Print(P089_IDACI3SecFactor,"P089_IDACI3SecFactor",rid, 8238)                                 

                Result = P092_IDACI3SecSubtotal  
   
                else
      
     
                    If (AcademyFilter = 17182 And FundingBasis = "Census") Or (AcademyFilter = 17183 And FundingBasis = "Census") then
                   
                    Dim P092_IDACI3SecSubtotalAPT As Decimal = _engine.GetDsDataValue(rid, 86122)
                    
                    Print(P092_IDACI3SecSubtotalAPT,"P092_IDACI3SecSubtotalAPT",rid, 8238)
    
                    Result = P092_IDACI3SecSubtotalAPT
                       
                    Else 
                    
                    exclude(rid, 8238)
                    
                    End If  
         
            End If
            
   
    
    
result = System.Math.Round(result, 2, MidpointRounding.AwayFromZero)
_engine._productResultCache.Add(__key, result)
Return result

End Function


Public Function GetProductResult_8241(rid As String) As Decimal
Dim __key = "8241" + rid
If (_engine._productResultCache.ContainsKey(__key)) = True Then Return _engine._productResultCache(__key)

    Dim result = 0
    
    Dim AcademyFilter As Decimal = GetProductResult_7582(rid) 
    Dim FundingBasis As String = _engine.GetDsDataValue(rid, 11187, "2017181")
     
    Print(AcademyFilter,"AcademyFilter",rid, 8241)
    Print(FundingBasis,"FundingBasis",rid, 8241)
    
    If FundingBasis = "Place" Then
    
    exclude(rid, 8241)
    
    Else

        If AcademyFilter = 17181 Or AcademyFilter = 17182 Or AcademyFilter = 17183 then
      
        Dim P092_IDACI3SecSubtotal As Decimal = GetProductResult_8238(rid) 
        Dim Days_Open As Decimal = GetProductResult_8151(rid) 
        Dim Year_Days As Decimal = 365                     

        Print(P092_IDACI3SecSubtotal,"P092 IDACI3SecSubtotal",rid, 8241)
        Print(Days_Open,"Days Open",rid, 8241)
        Print(Year_Days,"Year Days",rid, 8241)
    
        Result = (P092_IDACI3SecSubtotal) *Divide(Days_Open, Year_Days)
    
        Else
        
        Exclude(rid, 8241) 
        
        End If   
        
    End if 
      
    
result = System.Math.Round(result, 6, MidpointRounding.AwayFromZero)
_engine._productResultCache.Add(__key, result)
Return result
    
End Function


Public Function GetProductResult_8243(rid As String) As Decimal
Dim __key = "8243" + rid
If (_engine._productResultCache.ContainsKey(__key)) = True Then Return _engine._productResultCache(__key)

    Dim result = 0
    
    Dim AcademyFilter As Decimal = GetProductResult_7582(rid) 
    Dim FundingBasis As String = _engine.GetDsDataValue(rid, 11187, "2017181")
     
    Print(AcademyFilter,"AcademyFilter",rid, 8243)
    Print(FundingBasis,"FundingBasis",rid, 8243)
    
    If FundingBasis = "Place" Then
    
    exclude(rid, 8243) 
    
    Else 
      
        If AcademyFilter = 17181 Or AcademyFilter = 17182 Or AcademyFilter = 17183 then
        
        Dim ProviderName as string = _engine.GetDsDataValue(rid, 9070)
        Dim P097_IDACI4SecRate As Decimal = LAtoProv(_engine.GetLaDataValue(rid, 86512, Nothing)) 
            
        Result = P097_IDACI4SecRate 
        
        else
        
        exclude(rid, 8243)
        
        End if
        
    End if
    
    
result = System.Math.Round(result, 6, MidpointRounding.AwayFromZero)
_engine._productResultCache.Add(__key, result)
Return result
    
End Function


Public Function GetProductResult_8242(rid As String) As Decimal
Dim __key = "8242" + rid
If (_engine._productResultCache.ContainsKey(__key)) = True Then Return _engine._productResultCache(__key)

    Dim result = 0
    
    Dim AcademyFilter As Decimal = GetProductResult_7582(rid) 
    Dim FundingBasis As String = _engine.GetDsDataValue(rid, 11187, "2017181")
     
    Print(AcademyFilter,"AcademyFilter",rid, 8242)
    Print(FundingBasis,"FundingBasis",rid, 8242)
    
    If FundingBasis = "Place" Then
    
    exclude(rid, 8242) 

    Else

        If AcademyFilter = 17181 Or AcademyFilter = 17182 Or AcademyFilter = 17183 then
        
        Dim IDACI4SecCensus As Decimal = _engine.GetDsDataValue(rid, 86926)
        Dim IDACI4SecAdj As Decimal = _engine.GetDsDataValue(rid, 86042)
        Dim IDACI4SecAdjString as string = _engine.GetDsDataValue(rid, 86042)
        Dim IDACI4SecCensusString as string  = _engine.GetDsDataValue(rid, 86926)
        Dim LA_AV As Decimal = latoprov(_engine.GetLaDataValue(rid, 86839, Nothing))

        Print(LA_AV,"LA average",rid, 8242)  
        Print(IDACI4SecCensus,"IDACI 4 Sec Census", rid, 8242)
        Print(IDACI4SecAdj,"IDACI 4 Sec Adjusted", rid, 8242)   
    
            If string.IsNullOrEmpty(IDACI4SecCensusString) And string.IsNullOrEmpty(IDACI4SecAdjString) then
            
            Result = LA_AV
            
            Else
    
                If string.IsNullOrEmpty(IDACI4SecAdjString) THEN
                
                Result = IDACI4SecCensus
                
                Else
                
                Result = IDACI4SecAdj
                
                End if
                
            End if
    
            else
            
            exclude(rid, 8242)
            
            End if
            
    End if
    
    
result = System.Math.Round(result, 6, MidpointRounding.AwayFromZero)
_engine._productResultCache.Add(__key, result)
Return result
    
End Function


Public Function GetProductResult_8244(rid As String) As Decimal
Dim __key = "8244" + rid
If (_engine._productResultCache.ContainsKey(__key)) = True Then Return _engine._productResultCache(__key)

    Dim result = 0
    
    Dim AcademyFilter As Decimal = GetProductResult_7582(rid) 
    Dim FundingBasis As String = _engine.GetDsDataValue(rid, 11187, "2017181")
   
     
    Print(AcademyFilter,"AcademyFilter",rid, 8244)
    Print(FundingBasis,"FundingBasis",rid, 8244)
    
    If FundingBasis = "Place" Then
    
    exclude(rid, 8244) 
    
    Else

       
        
       
                
           
            
           
          
            End if
            
            print(FundingBasis, "FundingBasisType",rid, 8244)
            print(AcademyFilter, "AcademyFilter", rid, 8244)
      
                If AcademyFilter = 17181 Or (AcademyFilter = 17182 And FundingBasis = "Estimate") Or (AcademyFilter = 17183 And FundingBasis = "Estimate") then
                
                Dim P096_IDACI4SecPupils As Decimal = GetProductResult_7673(rid) 
                Dim P097_IDACI4SecRate As Decimal = GetProductResult_8243(rid) 
                Dim P095_IDACI4SecFactor As Decimal = GetProductResult_8242(rid) 
                Dim P098_IDACI4SecSubtotal As Decimal = P096_IDACI4SecPupils * P097_IDACI4SecRate * P095_IDACI4SecFactor
     
                Print(P096_IDACI4SecPupils,"P096_IDACI4SecPupils",rid, 8244)
                Print(P097_IDACI4SecRate,"P097_IDACI4SecRate",rid, 8244)
                Print(P095_IDACI4SecFactor,"P095_IDACI4SecFactor",rid, 8244)                                 

                Result = P098_IDACI4SecSubtotal  
                
                else
    
                    If (AcademyFilter = 17182 And FundingBasis = "Census") Or (AcademyFilter = 17183 And FundingBasis = "Census") then
                   
                    Dim P098_IDACI4SecSubtotalAPT As Decimal = _engine.GetDsDataValue(rid, 86124)
                    
                    Print(P098_IDACI4SecSubtotalAPT,"P098_IDACI4SecSubtotalAPT",rid, 8244)
    
                    Result = P098_IDACI4SecSubtotalAPT
                       
                    Else  
                    
                    exclude(rid, 8244)
                    
                    End If  
  
            End If  
            
   
    
    
result = System.Math.Round(result, 2, MidpointRounding.AwayFromZero)
_engine._productResultCache.Add(__key, result)
Return result
    
End Function


Public Function GetProductResult_8247(rid As String) As Decimal
Dim __key = "8247" + rid
If (_engine._productResultCache.ContainsKey(__key)) = True Then Return _engine._productResultCache(__key)

    Dim result = 0
    
    Dim AcademyFilter As Decimal = GetProductResult_7582(rid) 
    Dim FundingBasis As String = _engine.GetDsDataValue(rid, 11187, "2017181")
     
    Print(AcademyFilter,"AcademyFilter",rid, 8247)
    Print(FundingBasis,"FundingBasis",rid, 8247)
    
    If FundingBasis = "Place" Then
    
    exclude(rid, 8247)
    
    Else

        If AcademyFilter = 17181 Or AcademyFilter = 17182 Or AcademyFilter = 17183 then
      
        Dim P098_IDACI4SecSubtotal As Decimal = GetProductResult_8244(rid) 
        Dim Days_Open As Decimal = GetProductResult_8151(rid) 
        Dim Year_Days As Decimal = 365                     

        Print(P098_IDACI4SecSubtotal,"P098 IDACI4SecSubtotal",rid, 8247)
        Print(Days_Open,"Days Open",rid, 8247)
        Print(Year_Days,"Year Days",rid, 8247)
    
        Result = (P098_IDACI4SecSubtotal) *Divide(Days_Open, Year_Days)
    
        Else
        
        Exclude(rid, 8247)
        
        End If
        
    End if  
      
    
result = System.Math.Round(result, 6, MidpointRounding.AwayFromZero)
_engine._productResultCache.Add(__key, result)
Return result
    
End Function


Public Function GetProductResult_8249(rid As String) As Decimal
Dim __key = "8249" + rid
If (_engine._productResultCache.ContainsKey(__key)) = True Then Return _engine._productResultCache(__key)

    Dim result = 0
    
    Dim AcademyFilter As Decimal = GetProductResult_7582(rid) 
    Dim FundingBasis As String = _engine.GetDsDataValue(rid, 11187, "2017181")
     
    Print(AcademyFilter,"AcademyFilter",rid, 8249)
    Print(FundingBasis,"FundingBasis",rid, 8249)
    
    If FundingBasis = "Place" Then
    
    exclude(rid, 8249)
    
    Else
    
        If AcademyFilter = 17181 Or AcademyFilter = 17182 Or AcademyFilter = 17183 then
        
        Dim ProviderName as string = _engine.GetDsDataValue(rid, 9070)
        Dim P103_IDACI5SecRate As Decimal = LAtoProv(_engine.GetLaDataValue(rid, 86526, Nothing)) 
    
        Result = P103_IDACI5SecRate 
        
        else
        
        exclude(rid, 8249)
        
        End if
        
    End if
    
    
result = System.Math.Round(result, 6, MidpointRounding.AwayFromZero)
_engine._productResultCache.Add(__key, result)
Return result
    
End Function


Public Function GetProductResult_8248(rid As String) As Decimal
Dim __key = "8248" + rid
If (_engine._productResultCache.ContainsKey(__key)) = True Then Return _engine._productResultCache(__key)

    Dim result = 0
    
    Dim AcademyFilter As Decimal = GetProductResult_7582(rid) 
    Dim FundingBasis As String = _engine.GetDsDataValue(rid, 11187, "2017181")
     
    Print(AcademyFilter,"AcademyFilter",rid, 8248)
    Print(FundingBasis,"FundingBasis",rid, 8248)
    
    If FundingBasis = "Place" Then
    
    exclude(rid, 8248) 
    
    Else

        If AcademyFilter = 17181 Or AcademyFilter = 17182 Or AcademyFilter = 17183 then
        
        Dim IDACI5SecCensus As Decimal = _engine.GetDsDataValue(rid, 86928)
        Dim IDACI5SecAdj As Decimal = _engine.GetDsDataValue(rid, 86044)
        Dim IDACI5SecAdjString as string = _engine.GetDsDataValue(rid, 86044)
        Dim IDACI5SecCensusString as string  = _engine.GetDsDataValue(rid, 86928)
        Dim LA_AV As Decimal = latoprov(_engine.GetLaDataValue(rid, 86841, Nothing))    

        Print(LA_AV,"LA average",rid, 8248)   
        Print(IDACI5SecCensus,"IDACI 5 Sec Census", rid, 8248)
        Print(IDACI5SecAdj,"IDACI 5 Sec Adjusted", rid, 8248)   
    
        If string.IsNullOrEmpty(IDACI5SecCensusString) And string.IsNullOrEmpty(IDACI5SecAdjString) then
        
        Result = LA_AV
        
        Else
    
            If string.IsNullOrEmpty(IDACI5SecAdjString) THEN
            
            Result = IDACI5SecCensus
            
            Else
            
            Result = IDACI5SecAdj
            
            End if
            
        End if
    
        else
        
        exclude(rid, 8248)
        
    End if
    
    End if
    
    
result = System.Math.Round(result, 6, MidpointRounding.AwayFromZero)
_engine._productResultCache.Add(__key, result)
Return result
    
End Function



Public Function GetProductResult_8250(rid As String) As Decimal
Dim __key = "8250" + rid
If (_engine._productResultCache.ContainsKey(__key)) = True Then Return _engine._productResultCache(__key)

    Dim result = 0
    
    Dim AcademyFilter As Decimal = GetProductResult_7582(rid) 
    Dim FundingBasis As String = _engine.GetDsDataValue(rid, 11187, "2017181")
   
     
    Print(AcademyFilter,"AcademyFilter",rid, 8250)
    Print(FundingBasis,"FundingBasis",rid, 8250)
    
    If FundingBasis = "Place" Then
    
    exclude(rid, 8250) 
    
    Else

       
        
       
                
           
            
           
          
            End if
            
            print(FundingBasis, "FundingBasisType",rid, 8250)
            print(AcademyFilter, "AcademyFilter", rid, 8250)
      
                If AcademyFilter = 17181 Or (AcademyFilter = 17182 And FundingBasis = "Estimate") Or (AcademyFilter = 17183 And FundingBasis = "Estimate")   then   
                Dim P102_IDACI5SecPupils As Decimal = GetProductResult_7673(rid) 
                Dim P103_IDACI5SecRate As Decimal = GetProductResult_8249(rid) 
                Dim P101_IDACI5SecFactor As Decimal = GetProductResult_8248(rid) 
                Dim P104_IDACI5SecSubtotal As Decimal = P102_IDACI5SecPupils * P103_IDACI5SecRate * P101_IDACI5SecFactor
      
                Print(P102_IDACI5SecPupils,"P102_IDACI5SecPupils",rid, 8250)
                Print(P103_IDACI5SecRate,"P103_IDACI5SecRate",rid, 8250)
                Print(P101_IDACI5SecFactor,"P101_IDACI5SecFactor",rid, 8250)                                 

                Result = P104_IDACI5SecSubtotal
                
                else
      
                    If (AcademyFilter = 17182 And FundingBasis = "Census") Or (AcademyFilter = 17183 And FundingBasis = "Census") then
                   
                    Dim P104_IDACI5SecSubtotalAPT As Decimal = _engine.GetDsDataValue(rid, 86126) 
                    
                    Print(P104_IDACI5SecSubtotalAPT,"P104_IDACI5SecSubtotalAPT",rid, 8250)
    
                    Result = P104_IDACI5SecSubtotalAPT
                       
                    Else
                    
                    exclude(rid, 8250)
                    
                    End If  
      
                End If  
    
       
    
    
result = System.Math.Round(result, 2, MidpointRounding.AwayFromZero)
_engine._productResultCache.Add(__key, result)
Return result
    
End Function


Public Function GetProductResult_8253(rid As String) As Decimal
Dim __key = "8253" + rid
If (_engine._productResultCache.ContainsKey(__key)) = True Then Return _engine._productResultCache(__key)

    Dim result = 0
    
    Dim AcademyFilter As Decimal = GetProductResult_7582(rid) 
    Dim FundingBasis As String = _engine.GetDsDataValue(rid, 11187, "2017181")
     
    Print(AcademyFilter,"AcademyFilter",rid, 8253)
    Print(FundingBasis,"FundingBasis",rid, 8253)
    
    If FundingBasis = "Place" Then
    
    exclude(rid, 8253)
    
    Else

        If AcademyFilter = 17181 Or AcademyFilter = 17182 Or AcademyFilter = 17183 then
      
        Dim P104_IDACI5SecSubtotal As Decimal = GetProductResult_8250(rid) 
        Dim Days_Open As Decimal = GetProductResult_8151(rid) 
        Dim Year_Days As Decimal = 365                     

        Print(P104_IDACI5SecSubtotal,"P104 IDACI5SecSubtotal",rid, 8253)
        Print(Days_Open,"Days Open",rid, 8253)
        Print(Year_Days,"Year Days",rid, 8253)
    
        Result = (P104_IDACI5SecSubtotal) *Divide(Days_Open, Year_Days)
    
        Else
        
        Exclude(rid, 8253)  
        
        End If  
        
    End if  
      
    
result = System.Math.Round(result, 6, MidpointRounding.AwayFromZero)
_engine._productResultCache.Add(__key, result)
Return result
    
End Function


Public Function GetProductResult_8255(rid As String) As Decimal
Dim __key = "8255" + rid
If (_engine._productResultCache.ContainsKey(__key)) = True Then Return _engine._productResultCache(__key)

    Dim result = 0
    
    Dim AcademyFilter As Decimal = GetProductResult_7582(rid) 
    Dim FundingBasis As String = _engine.GetDsDataValue(rid, 11187, "2017181")
     
    Print(AcademyFilter,"AcademyFilter",rid, 8255)
    Print(FundingBasis,"FundingBasis",rid, 8255)
    
    If FundingBasis = "Place" Then
    
    exclude(rid, 8255)
    
    Else
     
        If AcademyFilter = 17181 Or AcademyFilter = 17182 Or AcademyFilter = 17183 then
        
        Dim ProviderName as string = _engine.GetDsDataValue(rid, 9070)
        Dim P109_IDACI6SecRate As Decimal = LAtoProv(_engine.GetLaDataValue(rid, 86540, Nothing)) 
    
        Result = P109_IDACI6SecRate 
        
        else
        
        exclude(rid, 8255)
        
        End if
        
    End if
    
    
result = System.Math.Round(result, 6, MidpointRounding.AwayFromZero)
_engine._productResultCache.Add(__key, result)
Return result
    
End Function


Public Function GetProductResult_8254(rid As String) As Decimal
Dim __key = "8254" + rid
If (_engine._productResultCache.ContainsKey(__key)) = True Then Return _engine._productResultCache(__key)

   Dim result = 0
   
   Dim AcademyFilter As Decimal = GetProductResult_7582(rid) 
   Dim FundingBasis As String = _engine.GetDsDataValue(rid, 11187, "2017181")
     
   Print(AcademyFilter,"AcademyFilter",rid, 8254)
   Print(FundingBasis,"FundingBasis",rid, 8254)
    
   If FundingBasis = "Place" Then
   
   exclude(rid, 8254) 
   
   Else

    If AcademyFilter = 17181 Or AcademyFilter = 17182 Or AcademyFilter = 17183 then
    
    Dim IDACI6SecCensus As Decimal = _engine.GetDsDataValue(rid, 86930)
    Dim IDACI6SecAdj As Decimal = _engine.GetDsDataValue(rid, 86046)
    Dim IDACI6SecAdjString as string = _engine.GetDsDataValue(rid, 86046)
    Dim IDACI6SecCensusString as string  = _engine.GetDsDataValue(rid, 86930)
    Dim LA_AV As Decimal = latoprov(_engine.GetLaDataValue(rid, 86843, Nothing))

    Print(LA_AV,"LA average",rid, 8254)     
    Print(IDACI6SecCensus,"IDACI 6 Sec Census", rid, 8254)
    Print(IDACI6SecAdj,"IDACI 6 Sec Adjusted", rid, 8254)
   
        If string.IsNullOrEmpty(IDACI6SecCensusString) And string.IsNullOrEmpty(IDACI6SecAdjString)  then
        
        Result = LA_AV
        
        Else
   
            If string.IsNullOrEmpty(IDACI6SecAdjString) THEN
            
            Result = IDACI6SecCensus
            
            Else
            
            Result = IDACI6SecAdj
            
            End if
            
        End if
   
        else
        
        exclude(rid, 8254)
        
        End if
        
   End if
   
   
result = System.Math.Round(result, 6, MidpointRounding.AwayFromZero)
_engine._productResultCache.Add(__key, result)
Return result
    
End Function



Public Function GetProductResult_8256(rid As String) As Decimal
Dim __key = "8256" + rid
If (_engine._productResultCache.ContainsKey(__key)) = True Then Return _engine._productResultCache(__key)

    Dim result = 0
    
    Dim AcademyFilter As Decimal = GetProductResult_7582(rid) 
    Dim FundingBasis As String = _engine.GetDsDataValue(rid, 11187, "2017181")
   
     
    Print(AcademyFilter,"AcademyFilter",rid, 8256)
    Print(FundingBasis,"FundingBasis",rid, 8256)
    
    If FundingBasis = "Place" Then
    
    exclude(rid, 8256)
    
    Else

       
        
       
                
           
            
           
          
            End if
            
            print(FundingBasis, "FundingBasisType",rid, 8256)
            print(AcademyFilter, "AcademyFilter", rid, 8256)
      
                If AcademyFilter = 17181 Or (AcademyFilter = 17182 And FundingBasis = "Estimate") Or (AcademyFilter = 17183 And FundingBasis = "Estimate")   then  
                
                Dim P108_IDACI6SecPupils As Decimal = GetProductResult_7673(rid) 
                Dim P109_IDACI6SecRate As Decimal = GetProductResult_8255(rid) 
                Dim P107_IDACI6SecFactor As Decimal = GetProductResult_8254(rid) 
                Dim P110_IDACI6SecSubtotal As Decimal = P108_IDACI6SecPupils * P109_IDACI6SecRate * P107_IDACI6SecFactor
     
                Print(P108_IDACI6SecPupils,"P108_IDACI6SecPupils",rid, 8256)
                Print(P109_IDACI6SecRate,"P109_IDACI6SecRate",rid, 8256)
                Print(P107_IDACI6SecFactor,"P107_IDACI6SecFactor",rid, 8256)                                 

                Result = P110_IDACI6SecSubtotal  
    
                else
  
                    If (AcademyFilter = 17182 And FundingBasis = "Census") Or (AcademyFilter = 17183 And FundingBasis = "Census") then
                   
                    Dim P110_IDACI6SecSubtotalAPT As Decimal = _engine.GetDsDataValue(rid, 86128)
                    
                    Print(P110_IDACI6SecSubtotalAPT,"P110_IDACI6SecSubtotalAPT",rid, 8256)
    
                    Result = P110_IDACI6SecSubtotalAPT
                       
                    Else
                    
                    exclude(rid, 8256)
                    
                    End If  
    
                End If  
       
    
    
result = System.Math.Round(result, 2, MidpointRounding.AwayFromZero)
_engine._productResultCache.Add(__key, result)
Return result
    
End Function


Public Function GetProductResult_8259(rid As String) As Decimal
Dim __key = "8259" + rid
If (_engine._productResultCache.ContainsKey(__key)) = True Then Return _engine._productResultCache(__key)

    Dim result = 0
    
    Dim AcademyFilter As Decimal = GetProductResult_7582(rid) 
    Dim FundingBasis As String = _engine.GetDsDataValue(rid, 11187, "2017181")
     
    Print(AcademyFilter,"AcademyFilter",rid, 8259)
    Print(FundingBasis,"FundingBasis",rid, 8259)
    
    If FundingBasis = "Place" Then
    
    exclude(rid, 8259)
    
    Else

        If AcademyFilter = 17181 Or AcademyFilter = 17182 Or AcademyFilter = 17183 then
      
        Dim P110_IDACI6SecSubtotal As Decimal = GetProductResult_8256(rid) 
        Dim Days_Open As Decimal = GetProductResult_8151(rid) 
        Dim Year_Days As Decimal = 365                     

        Print(P110_IDACI6SecSubtotal,"P110 IDACI6SecSubtotal",rid, 8259)
        Print(Days_Open,"Days Open",rid, 8259)
        Print(Year_Days,"Year Days",rid, 8259)
    
        Result = (P110_IDACI6SecSubtotal) *Divide(Days_Open, Year_Days)
    
        Else
        
        Exclude(rid, 8259)  
        
        End If  
        
    End if   
      
    
result = System.Math.Round(result, 6, MidpointRounding.AwayFromZero)
_engine._productResultCache.Add(__key, result)
Return result
    
End Function


Public Function GetProductResult_7668(rid As String) As Decimal
Dim __key = "7668" + rid
If (_engine._productResultCache.ContainsKey(__key)) = True Then Return _engine._productResultCache(__key)

 Dim result = 0
 
 
 
 
 
 Dim AcadFilter As Decimal = GetProductResult_7582(rid) 
 Dim TotalPlacesAPT As Decimal =_engine.GetDsDataValue(rid, 86365)
 Dim IsNull As Boolean
 IsNull = IIf(_engine.GetApprovedDsDataValue(rid, 86365), false, true) 
 Dim NOR_Inp_Adj As Decimal = _engine.GetDsDataValue(rid, 85998) 
 Dim Pre16HNData As Decimal = _engine.GetDsDataValue(rid, 87244)
 Dim Pre16APData As Decimal = _engine.GetDsDataValue(rid, 87246)
 Dim TotalPlacesHNData As Decimal = Pre16HNData + Pre16APData
 Dim NOR_Pri As Decimal = GetProductResult_7646(rid) 
 Dim RU_NOR As Decimal = GetProductResult_7645(rid) 
 Dim APT_HN_Pri As Decimal = GetProductResult_7653(rid)  
 Dim HND_AP_Primary As Decimal= GetProductResult_7659(rid) 
 Dim HND_HNP_Pri As Decimal = GetProductResult_7656(rid)  
 Dim HND_HN_Pri As Decimal = HND_HNP_Pri + HND_AP_Primary 
 Dim HN_Unit As Decimal = GetProductResult_7638(rid)  
 Dim HN_to_Deduct As Decimal
 Dim FundingBasis As Decimal = GetProductResult_7594(rid) 
 




 
 If currentscenario.periodid = 2017181 And (AcadFilter = 17181 Or AcadFilter = 17182 Or AcadFilter = 17183) then
 If (IsNull = True Or TotalPlacesAPT = 0) And TotalPlacesHNData > 0 Then
 HN_to_deduct = 0
 Else If TotalPlacesAPT > TotalPlacesHNData Then
 HN_to_deduct = HND_HN_Pri 
 Else 
 HN_to_deduct = APT_HN_Pri
 End If 
Else Exclude(rid, 7668)
End If 
 
 
 Print(TotalPlacesAPT, "Places from APT",rid, 7668)
 Print(TotalPlacesHNData, "Places from HN Data",rid, 7668)
 Print(APT_HN_Pri, "APT HN Pri", rid, 7668)
 Print(HND_AP_Primary, "HNP Pri", rid, 7668)
 Print(HND_HNP_Pri, "HND APPri", rid, 7668)
 Print(HND_HN_Pri, "HND Pri", rid, 7668)
 Print(HN_Unit,"HN unit",rid, 7668)
 Print(HN_to_deduct, "HN todeduct", rid, 7668)
 

If currentscenario.periodid = 2017181 And (AcadFilter = 17182 Or AcadFilter = 17183) And FundingBasis = 1 Then 
 Result = 0
 Else 
 Result = HN_to_deduct 
End If 

 
 
_engine._productResultCache.Add(__key, result)
Return result
End Function


Public Function GetProductResult_7670(rid As String) As Decimal
Dim __key = "7670" + rid
If (_engine._productResultCache.ContainsKey(__key)) = True Then Return _engine._productResultCache(__key)

 Dim result = 0
 
 Dim AcadFilter As Decimal = GetProductResult_7582(rid) 
 Dim TotalPlacesAPT As Decimal =_engine.GetDsDataValue(rid, 86365)
 Dim IsNull As Boolean
 IsNull = IIf(_engine.GetApprovedDsDataValue(rid, 86365), false, true) 
 Dim NOR_Inp_Adj As Decimal = _engine.GetDsDataValue(rid, 86006) 
 Dim Pre16HNData As Decimal = _engine.GetDsDataValue(rid, 87244)
 Dim Pre16APData As Decimal = _engine.GetDsDataValue(rid, 87246)
 Dim TotalPlacesHNData As Decimal = Pre16HNData + Pre16APData
 Dim NOR_KS3 As Decimal = GetProductResult_7651(rid) 
 Dim APT_HN_KS3 As Decimal = GetProductResult_7654(rid)  
 Dim HND_AP_KS3 As Decimal= GetProductResult_7660(rid) 
 Dim HND_HNP_KS3 As Decimal = GetProductResult_7657(rid)  
 Dim HND_HN_KS3 As Decimal = HND_AP_KS3 + HND_HNP_KS3 
 Dim HN_Unit As Decimal = GetProductResult_7638(rid)  
 Dim Fundingbasis As String = GetProductResult_7594(rid) 
 Dim HN_to_Deduct As Decimal




If currentscenario.periodid = 2017181 And (AcadFilter = 17181 Or AcadFilter = 17182 Or AcadFilter = 17183) then
 If (IsNull = True Or TotalPlacesAPT = 0) And TotalPlacesHNData > 0 Then
 HN_to_deduct = 0
 Else If TotalPlacesAPT > TotalPlacesHNData Then
 HN_to_deduct = HND_HN_KS3 
 Else 
 HN_to_deduct = APT_HN_KS3
 End If 
Else Exclude(rid, 7670)
End If 
 
 
 Print(Fundingbasis, "funding basis", rid, 7670)
 Print(TotalPlacesAPT, "APT total place count",rid, 7670)
 Print(NOR_KS3, "NOR KS3", rid, 7670)
 Print(APT_HN_KS3, "APTKS3", rid, 7670)
 Print(HND_AP_KS3, "HNP KS3", rid, 7670)
 Print(HND_HNP_KS3, "HND APKS3", rid, 7670)
 Print(HND_HN_KS3, "HND KS3", rid, 7670)
 Print(HN_Unit,"HN unit",rid, 7670)
 Print(HN_to_deduct, "HN todeduct", rid, 7670)
 
 If currentscenario.periodid = 2017181 And (AcadFilter = 17182 Or AcadFilter = 17183) And Fundingbasis = 1 Then 
 Result = 0
 Else 
 Result = HN_to_deduct 
End If 
 
 
_engine._productResultCache.Add(__key, Result)
Return Result
End Function




Public Function GetProductResult_7672(rid As String) As Decimal
Dim __key = "7672" + rid
If (_engine._productResultCache.ContainsKey(__key)) = True Then Return _engine._productResultCache(__key)

 Dim result = 0
 
 
 
 
 
 Dim AcadFilter As Decimal = GetProductResult_7582(rid) 
 Dim TotalPlacesAPT As Decimal =_engine.GetDsDataValue(rid, 86365)
 Dim IsNull As Boolean
 IsNull = IIf(_engine.GetApprovedDsDataValue(rid, 86365), false, true) 
 Dim NOR_Inp_Adj As Decimal = _engine.GetDsDataValue(rid, 86008) 
 Dim Pre16HNData As Decimal = _engine.GetDsDataValue(rid, 87244)
 Dim Pre16APData As Decimal = _engine.GetDsDataValue(rid, 87246)
 Dim TotalPlacesHNData As Decimal = Pre16HNData + Pre16APData
 Dim NOR_KS4 As Decimal = GetProductResult_7652(rid) 
 Dim APT_HN_KS4 As Decimal = GetProductResult_7655(rid)  
 Dim HND_AP_KS4 As Decimal= GetProductResult_7661(rid) 
 Dim HND_HNP_KS4 As Decimal = GetProductResult_7658(rid)  
 Dim HND_HN_KS4 As Decimal = GetProductResult_7658(rid)  + GetProductResult_7661(rid)  
 Dim HN_Unit As Decimal = GetProductResult_7638(rid)  
 Dim Fundingbasis As Decimal = GetProductResult_7594(rid) 
 Dim HN_to_Deduct As Decimal


If currentscenario.periodid = 2017181 And (AcadFilter = 17181 Or AcadFilter = 171872 Or AcadFilter = 17183) then
 If (IsNull = True Or TotalPlacesAPT = 0) And TotalPlacesHNData > 0 Then
 HN_to_deduct = 0
 Else If TotalPlacesAPT > TotalPlacesHNData Then
 HN_to_deduct = HND_HN_KS4 
 Else 
 HN_to_deduct = APT_HN_KS4
 End If 
Else Exclude(rid, 7672)
End If 


 print(TotalPlacesAPT, "Tot APT places", rid, 7672)
 print(TotalPlacesHNData, "Total HN Dataset places", rid, 7672)
 Print(NOR_KS4, "NOR KS4", rid, 7672)
 Print(APT_HN_KS4, "APTKS4", rid, 7672)
 Print(HND_AP_KS4, "HNP KS4", rid, 7672)
 Print(HND_HNP_KS4, "HND APKS4", rid, 7672)
 Print(HND_HN_KS4, "HND KS4", rid, 7672)
 Print(HN_Unit,"HN unit",rid, 7672)
 Print(HN_to_deduct, "HN todeduct", rid, 7672)
 
If currentscenario.periodid = 2017181 And (AcadFilter = 17182 Or AcadFilter = 17183) And Fundingbasis = 1 Then 
 Result = 0
 Else 
 Result = HN_to_deduct 
End If 
 
 
_engine._productResultCache.Add(__key, result)
Return result
End Function



Public Function GetProductResult_7676(rid As String) As Decimal
Dim __key = "7676" + rid
If (_engine._productResultCache.ContainsKey(__key)) = True Then Return _engine._productResultCache(__key)

 Dim result = 0
 
 
 Dim AcadFilter As Decimal = GetProductResult_7582(rid) 
 Dim HN_Pri_Deduct As Decimal = GetProductResult_7668(rid) 
 Dim HN_KS3_Deduct As Decimal = GetProductResult_7670(rid) 
 Dim HN_KS4_Deduct As Decimal = GetProductResult_7672(rid) 
 Dim HN_APT_Pri As Decimal = _engine.GetDsDataValue(rid, 86367)
 Dim HN_APT_KS3 As Decimal = _engine.GetDsDataValue(rid, 86369)
 Dim HN_APT_KS4 As Decimal = _engine.GetDsDataValue(rid, 86371)

 Print(HN_Pri_Deduct, "Pri Hn Ded", rid, 7676)
 Print(HN_KS3_Deduct, "KS3 Hn Ded", rid, 7676) 
 Print(HN_KS4_Deduct, "KS4 Hn Ded", rid, 7676)
 Print(HN_APT_Pri, "KS4 Hn Ded", rid, 7676) 
 
 
If currentscenario.periodid = 2017181 And AcadFilter = 17181 Then 
 

 Result = HN_Pri_Deduct + HN_KS3_Deduct + HN_KS4_Deduct
 
 ElseIf currentscenario.periodid = 2017181 And (AcadFilter = 17182 Or AcadFilter = 17183) Then 
 
 Result = HN_APT_Pri + HN_APT_KS3 + HN_APT_KS4
 

 Else Exclude(rid, 7676)
End If 



 
_engine._productResultCache.Add(__key, result)
Return result
End Function


Public Function GetProductResult_7675(rid As String) As Decimal
Dim __key = "7675" + rid
If (_engine._productResultCache.ContainsKey(__key)) = True Then Return _engine._productResultCache(__key)

 Dim result = 0 
 
 
 Dim AcadFilter = GetProductResult_7582(rid) 
 Dim NOR_Pri_SBS As Decimal = GetProductResult_7667(rid) 
 Dim NOR_Sec_SBS As Decimal = GetProductResult_7673(rid) 
 Dim HN_Deductions As Decimal = GetProductResult_7676(rid) 
 
 Print(NOR_Pri_SBS, "Pri NOR", rid, 7675)
 Print(NOR_Sec_SBS, "Sec NOR", rid, 7675) 
 
If currentscenario.periodid = 2017181 And AcadFilter = 17181 then 
 Result = NOR_Pri_SBS + NOR_Sec_SBS
Else If currentscenario.periodid = 2017181 And (AcadFilter = 17182 Or AcadFilter = 17183) then 
 Result = NOR_Pri_SBS + NOR_Sec_SBS - HN_Deductions
 Else Exclude(rid, 7675)
End If 

 
_engine._productResultCache.Add(__key, result)
Return result
End Function


Public Function GetProductResult_8261(rid As String) As Decimal
Dim __key = "8261" + rid
If (_engine._productResultCache.ContainsKey(__key)) = True Then Return _engine._productResultCache(__key)

    Dim result = 0
    
    Dim AcademyFilter As Decimal = GetProductResult_7582(rid) 
    Dim FundingBasis As String = _engine.GetDsDataValue(rid, 11187, "2017181")
     
    Print(AcademyFilter,"AcademyFilter",rid, 8261)
    Print(FundingBasis,"FundingBasis",rid, 8261)
    
    If FundingBasis = "Place" Then
    
    exclude(rid, 8261) 
    
    Else

        If AcademyFilter = 17181 Or AcademyFilter = 17182 Or AcademyFilter = 17183 then
        
        Dim  provider As String = _engine.GetDsDataValue(rid, 9070)
        Dim LACRate As Decimal = LAtoProv(_engine.GetLaDataValue(rid, 86558, Nothing))
        
        result =  LACRate
       
        Else 
        
        exclude(rid, 8261)
        
        End if
        
     End if
     
    
result = System.Math.Round(result, 6, MidpointRounding.AwayFromZero)
_engine._productResultCache.Add(__key, result)
Return result
    
End Function


Public Function GetProductResult_8260(rid As String) As Decimal
Dim __key = "8260" + rid
If (_engine._productResultCache.ContainsKey(__key)) = True Then Return _engine._productResultCache(__key)

    Dim result = 0
    
    Dim AcademyFilter As Decimal = GetProductResult_7582(rid) 
    Dim FundingBasis As String = _engine.GetDsDataValue(rid, 11187, "2017181")
     
    Print(AcademyFilter,"AcademyFilter",rid, 8260)
    Print(FundingBasis,"FundingBasis",rid, 8260)
    
    If FundingBasis = "Place" Then
    
    exclude(rid, 8260) 
    
    Else

        If AcademyFilter = 17181 Or AcademyFilter = 17182 Or AcademyFilter = 17183 then
        
        Dim LACCensus As Decimal = _engine.GetDsDataValue(rid, 86944)
        Dim LACAdj As Decimal = _engine.GetDsDataValue(rid, 86060)
        Dim LACAdjString as string = _engine.GetDsDataValue(rid, 86060)
        Dim LACCensusString as string  = _engine.GetDsDataValue(rid, 86944)
        Dim LA_AV As Decimal = latoprov(_engine.GetLaDataValue(rid, 86857, Nothing))

        Print(LA_AV, "LA average", rid, 8260)
        Print(LACCensus, "LACCensus", rid, 8260)
        Print(LACAdj, "EAL 1 Pri Adjusted", rid, 8260)  
   
            If string.IsNullOrEmpty(LACAdjString) THEN
            
            Result = LACCensus
            
            Else
            
            Result = LACAdj
            
            End if
            
            else
            
            exclude(rid, 8260)
            
        End if
        
    End if
   
    
result = System.Math.Round(result, 6, MidpointRounding.AwayFromZero)
_engine._productResultCache.Add(__key, result)
Return result
    
End Function




Public Function GetProductResult_8262(rid As String) As Decimal
Dim __key = "8262" + rid
If (_engine._productResultCache.ContainsKey(__key)) = True Then Return _engine._productResultCache(__key)

    Dim result = 0
    
    Dim AcademyFilter As Decimal = GetProductResult_7582(rid) 
    Dim FundingBasis As String = _engine.GetDsDataValue(rid, 11187, "2017181")
   
    
    If FundingBasis = "Place" Then
    
    exclude(rid, 8262) 
    
    Else
    
       
        
       
        
           
            
           
            
            End if
            
            print(FundingBasis, "FundingBasisType",rid, 8262)
            print(AcademyFilter, "AcademyFilter", rid, 8262)
        
                If AcademyFilter = 17181 Or (AcademyFilter = 17182 And FundingBasis = "Estimate") Or (AcademyFilter = 17183 And FundingBasis = "Estimate")   then
                
                Dim P115_LACPupils As Decimal = GetProductResult_7675(rid) 
                Dim P116_LACRate As Decimal = GetProductResult_8261(rid) 
                Dim P114_LACFactor As Decimal = GetProductResult_8260(rid) 
                Dim P117_LACSubtotal As Decimal = P115_LACPupils * P116_LACRate * P114_LACFactor
       
                Print(P115_LACPupils,"P115_LACPupils",rid, 8262)
                Print(P116_LACRate,"P116_LACRate",rid, 8262)
                Print(P114_LACFactor,"P114_LACFactor",rid, 8262)     
         
                Result = P117_LACSubtotal 
    
                Else 
    
                   
                   
                    If  (AcademyFilter = 17182 And FundingBasis = "Census") Or (AcademyFilter = 17183 And FundingBasis = "Census") then
                   
                    Dim P117_LACSubtotalAPT As Decimal = _engine.GetDsDataValue(rid, 86134) 
                    
                    Print(P117_LACSubtotalAPT,"P117_LACSubtotalAPT",rid, 8262)
    
                    Result = P117_LACSubtotalAPT
                       
                    Else 
                    
                    exclude(rid, 8262)

                    End If  
     
                End If 
                
           
            
    
result = System.Math.Round(result, 2, MidpointRounding.AwayFromZero)
_engine._productResultCache.Add(__key, result)
Return result
    
End Function




 Public Function GetProductResult_8265(rid As String) As Decimal
Dim __key = "8265" + rid
If (_engine._productResultCache.ContainsKey(__key)) = True Then Return _engine._productResultCache(__key)

    Dim result = 0
    
    Dim AcademyFilter As Decimal = GetProductResult_7582(rid) 
    Dim FundingBasis As String = _engine.GetDsDataValue(rid, 11187, "2017181")
     
    Print(AcademyFilter,"AcademyFilter",rid, 8265)
    Print(FundingBasis,"FundingBasis",rid, 8265)
    
    If FundingBasis = "Place" Then
    
    exclude(rid, 8265) 
    
    Else

        If AcademyFilter = 17181 Or AcademyFilter = 17182 Or AcademyFilter = 17183 then

        Dim P117_LACSubtotal As Decimal = GetProductResult_8262(rid) 
        Dim Days_Open As Decimal = GetProductResult_8151(rid) 
        Dim Year_Days As Decimal = 365
        
        print(Days_Open, "Days_Open",rid, 8265)                          
        print(Year_Days, "Year_Days",rid, 8265)                           

        Result = (P117_LACSubtotal) *Divide(Days_Open, Year_Days)
    
        Else 
        
        Exclude(rid, 8265)  
        
        End If
        
     End if
    
    
_engine._productResultCache.Add(__key, result)
Return result
    
End Function


Public Function GetProductResult_8270(rid As String) As Decimal
Dim __key = "8270" + rid
If (_engine._productResultCache.ContainsKey(__key)) = True Then Return _engine._productResultCache(__key)

    Dim result = 0
    
    Dim AcademyFilter As Decimal = GetProductResult_7582(rid) 
    Dim FundingBasis As String = _engine.GetDsDataValue(rid, 11187, "2017181")
     
    Print(AcademyFilter,"AcademyFilter",rid, 8270)
    Print(FundingBasis,"FundingBasis",rid, 8270)
    
    If FundingBasis = "Place" Then
    
    exclude(rid, 8270)
    
    Else
    
        If AcademyFilter = 17181 Or AcademyFilter = 17182 Or AcademyFilter = 17183 then
        
        Dim PPAY1to2ProportionUnderCensus As Decimal = _engine.GetDsDataValue(rid, 86946)
        Dim PPAY1to2ProportionUnderAdj As Decimal = _engine.GetDsDataValue(rid, 86062)
        Dim PPAY1to2ProportionUnderAdjString As String = _engine.GetDsDataValue(rid, 86062)
        Dim PPAY1to2ProportionUnderCensusString As String = _engine.GetDsDataValue(rid, 86946)
        Dim LA_AV As Decimal = LAtoProv(_engine.GetLaDataValue(rid, 86859, Nothing))
    
        Print(LA_AV,"LA average",rid, 8270)
        Print(PPAY1to2ProportionUnderCensus,"PPAY1to2ProportionUnderCensus",rid, 8270)
        Print(PPAY1to2ProportionUnderAdj,"PPAY1to2ProportionUnderAdj",rid, 8270)
    
        If string.IsNullOrEmpty(PPAY1to2ProportionUnderCensusString) And String.IsNullOrEmpty(PPAY1to2ProportionUnderAdjString)  then
        
        Result = LA_AV
        
        Else
      
            If String.IsNullOrEmpty(PPAY1to2ProportionUnderAdjString) then
            
            Result = PPAY1to2ProportionUnderCensus
            
            Else 
            
            Result = PPAY1to2ProportionUnderAdj
            
            End If
            
        End if
    
        Else 
        
        exclude(rid, 8270)
        
        End If
        
    End if
    
    
result = System.Math.Round(result, 6, MidpointRounding.AwayFromZero)
_engine._productResultCache.Add(__key, result)
Return result

End Function 
   


Public Function GetProductResult_12254(rid As String) As Decimal
 Dim result As Decimal = 0
 
Dim __key = "12254" + rid
If (_engine._productResultCache.ContainsKey(__key)) = True Then Return _engine._productResultCache(__key)

 Dim EstNOR_Y1 As Decimal = _engine.GetDsDataValue(rid, 87274)
 Dim EstNOR_Y2 As Decimal = _engine.GetDsDataValue(rid, 87276)
 Dim EstNOR_Y3 As Decimal = _engine.GetDsDataValue(rid, 87278)
 Dim EstNOR_Y4 As Decimal = _engine.GetDsDataValue(rid, 87280)
 
 Print(EstNOR_Y1,"Year 1",rid, 12254)
 Print(EstNOR_Y2,"Year 2",rid, 12254)
 Print(EstNOR_Y3,"Year 3",rid, 12254)
 Print(EstNOR_Y4,"Year 4",rid, 12254)
 
 Result = (EstNOR_Y1 + EstNOR_Y2 + EstNOR_Y3 + EstNOR_Y4) 
 
 
_engine._productResultCache.Add(__key, result)
Return result
 
End Function


Public Function GetProductResult_7647(rid As String) As Decimal
Dim __key = "7647" + rid
If (_engine._productResultCache.ContainsKey(__key)) = True Then Return _engine._productResultCache(__key)

 Dim result = 0
 
 
 
 

 Dim DateOpened as date = _engine.GetDsDataValue(rid, 9077)
 Dim IsNull As Boolean
 IsNull = IIf(_engine.GetApprovedDsDataValue(rid, 86000), false, true) 
 Dim NOR_Inp_Adj As Decimal = _engine.GetDsDataValue(rid, 86000)
 Dim NOR_Y1Y4_Cen As Decimal = _engine.GetDsDataValue(rid, 87320)
 Dim NOR_Y1Y4_Est As Decimal = GetProductResult_12254(rid) 
 Dim NOR_Y1Y4_APT As Decimal = _engine.GetDsDataValue(rid, 86274)
 Dim AcadFilter As Decimal = GetProductResult_7582(rid) 
 Dim FundingBasis as string = GetProductResult_7594(rid) 
 Dim NOR_Y1_Y4 As Decimal
 Dim Guaranteed_Numbers As string = _engine.GetDsDataValue(rid, 88442)
 Dim InpAdj_NOR As Decimal = _engine.GetDsDataValue(rid, 85996)
 Dim RFDC_NOR As Decimal = GetProductResult_10460(rid) 
 

 
 Print(IsNull, "Input Adj Null check", rid, 7647)
 Print(NOR_Inp_Adj, "NOR InputsAdj", rid, 7647)
 Print(NOR_Y1Y4_Cen,"NORY1Y4 Census",rid, 7647)
 Print(NOR_Y1Y4_Est,"NORY1Y4 Estimate",rid, 7647)
 Print(FundingBasis,"Funding Basis",rid, 7647)
 Print(Guaranteed_Numbers, "Guaranteed_Numbers",rid, 7647)
 Print(InpAdj_NOR, "InpAdj_NOR",rid, 7647)
 Print(RFDC_NOR, "RFDC_NOR",rid, 7647)
 
 
 
If currentscenario.periodid = 2017181 And AcadFilter = 17181 then 
 If FundingBasis = 1 And IsNull = False Then
 NOR_Y1_Y4 = NOR_Inp_Adj
 ElseIf FundingBasis = 1 And IsNull = True Then
 NOR_Y1_Y4 = NOR_Y1Y4_Cen 
 ElseIf (FundingBasis = 2 And Guaranteed_Numbers = "Yes" And InpAdj_NOR > RFDC_NOR) Then
 NOR_Y1_Y4 = NOR_Inp_Adj
 ElseIf FundingBasis = 2 Then
 NOR_Y1_Y4 = NOR_Y1Y4_Est
 End If 
 ElseIf currentscenario.periodid = 2017181 And (AcadFilter = 17182 Or AcadFilter = 17183) Then
 If FundingBasis = 1 Then
 NOR_Y1_Y4 = NOR_Y1Y4_APT
 ElseIf (FundingBasis = 2 And Guaranteed_Numbers = "Yes" And InpAdj_NOR > RFDC_NOR) Then
 NOR_Y1_Y4 = NOR_Inp_Adj
 ElseIf FundingBasis = 2 Then
 NOR_Y1_Y4 = NOR_Y1Y4_Est
 End If 
 Else Exclude(rid, 7647) 
End If

 result = NOR_Y1_Y4
 
_engine._productResultCache.Add(__key, result)
Return result
 
End Function


Public Function GetProductResult_8272(rid As String) As Decimal
Dim __key = "8272" + rid
If (_engine._productResultCache.ContainsKey(__key)) = True Then Return _engine._productResultCache(__key)

    Dim result = 0
    
    Dim AcademyFilter As Decimal = GetProductResult_7582(rid) 
    Dim FundingBasis As String = _engine.GetDsDataValue(rid, 11187, "2017181")
     
    Print(AcademyFilter,"AcademyFilter",rid, 8272)
    Print(FundingBasis,"FundingBasis",rid, 8272)
    
    If FundingBasis = "Place" Then
    
    exclude(rid, 8272) 
    
    Else
    
        If AcademyFilter = 17181 Or AcademyFilter = 17182 Or AcademyFilter = 17183 then
        
        Dim PPAY1to2NOR As Decimal = GetProductResult_7647(rid) 
    
        Print(PPAY1to2NOR,"P125_PPAY1to2NOR",rid, 8272)
    
        Result = PPAY1to2NOR
        
        else
        
        exclude(rid, 8272)
     
        End if
    
    End if
    
    
_engine._productResultCache.Add(__key, result)
Return result
    
End Function


Public Function GetProductResult_8275(rid As String) As Decimal
Dim __key = "8275" + rid
If (_engine._productResultCache.ContainsKey(__key)) = True Then Return _engine._productResultCache(__key)

    Dim result = 0
    
    Dim AcademyFilter As Decimal = GetProductResult_7582(rid) 
    Dim FundingBasis As String = _engine.GetDsDataValue(rid, 11187, "2017181")
     
    Print(AcademyFilter,"AcademyFilter",rid, 8275)
    Print(FundingBasis,"FundingBasis",rid, 8275)
    
    If FundingBasis = "Place" Then
    
    exclude(rid, 8275)
    
    Else
    
        If AcademyFilter = 17181 Or AcademyFilter = 17182 Or AcademyFilter = 17183 then
        
        Dim provider As String = _engine.GetDsDataValue(rid, 9070)
        Dim PPAWeighting As Decimal = LaToProv(_engine.GetLaDataValue(rid, 86608, Nothing))
          
       
    
        Result = PPAWeighting
        
        else
        
        exclude(rid, 8275)
        
        End if
        
    End if
    
    
result = System.Math.Round(result, 6, MidpointRounding.AwayFromZero)
_engine._productResultCache.Add(__key, result)
Return result
    
End Function



Public Function GetProductResult_8277(rid As String) As Decimal
Dim __key = "8277" + rid
If (_engine._productResultCache.ContainsKey(__key)) = True Then Return _engine._productResultCache(__key)

    Dim result = 0
    
    Dim AcademyFilter As Decimal = GetProductResult_7582(rid) 
    Dim FundingBasis As String = _engine.GetDsDataValue(rid, 11187, "2017181")
     
    Print(AcademyFilter,"AcademyFilter",rid, 8277)
    Print(FundingBasis,"FundingBasis",rid, 8277)
    
    If FundingBasis = "Place" Then
    
    exclude(rid, 8277) 
    
    Else
    
        If AcademyFilter = 17181 Or AcademyFilter = 17182 Or AcademyFilter = 17183 then
        
        Dim PPAY1to2ProportionUnder As Decimal = GetProductResult_8270(rid) 
        Dim PPAY1to2NOR As Decimal = GetProductResult_8272(rid) 
        Dim P128_PPAWeighting As Decimal = GetProductResult_8275(rid) 
        Dim PPAPupilsY1to2NotAchieving As Decimal = PPAY1to2ProportionUnder * P128_PPAWeighting * PPAY1to2NOR
    
        Print(PPAY1to2ProportionUnder,"P123_PPAY1to2ProportionUnder",rid, 8277)
        Print(PPAY1to2NOR,"P125_PPAY1to2NOR",rid, 8277)
        Print(P128_PPAWeighting,"P128_PPAWeighting",rid, 8277)
        Print(PPAPupilsY1to2NotAchieving,"P130_PPAPupilsY1to2NotAchieving",rid, 8277)
    
        Result =   PPAPupilsY1to2NotAchieving
    
        else
        
        exclude(rid, 8277)
        
        End if
        
    End if
    
    
result = System.Math.Round(result, 6, MidpointRounding.AwayFromZero)
_engine._productResultCache.Add(__key, result)
Return result
    
End Function



Public Function GetProductResult_8266(rid As String) As Decimal
Dim __key = "8266" + rid
If (_engine._productResultCache.ContainsKey(__key)) = True Then Return _engine._productResultCache(__key)

    Dim result = 0
    
    Dim AcademyFilter As Decimal = GetProductResult_7582(rid) 
    Dim FundingBasis As String = _engine.GetDsDataValue(rid, 11187, "2017181")
 
    
    If FundingBasis = "Place" Then
    
    exclude(rid, 8266)
    
    Else
    
        If AcademyFilter = 17181 Or AcademyFilter = 17182 Or AcademyFilter = 17183 then
        
        Dim provider As String = _engine.GetDsDataValue(rid, 9070)
        Dim PPAindicator_String As string = LaToProv(_engine.GetLaDataValue(rid, 86606, Nothing))
        Dim PPAindicator As Decimal 

    Print(AcademyFilter,"AcademyFilter",rid, 8266)
    Print(FundingBasis,"FundingBasis",rid, 8266)
 Print(PPAindicator_String,"Indicator check",rid, 8266)
    
            If PPAindicator_String = "Low Attainment % old FSP 73" Then 
            
            PPAindicator = 73
            
            End if
        
                If PPAindicator_String = "Low Attainment % old FSP 78" Then
                    
                PPAindicator = 78
                
                End If 
            
                    If PPAindicator_String = "NA" then
                    
                    PPAindicator = 0
                    
                    End if
                   
                    Result =   PPAindicator
    
                    Else 
                    
                    exclude(rid, 8266)
     
                End If
        
        End if    
    
_engine._productResultCache.Add(__key, result)
Return result

     
 
End Function


Public Function GetProductResult_8267(rid As String) As Decimal
Dim __key = "8267" + rid
If (_engine._productResultCache.ContainsKey(__key)) = True Then Return _engine._productResultCache(__key)

Dim result = 0

    Dim AcademyFilter As Decimal = GetProductResult_7582(rid) 
    Dim FundingBasis As String = _engine.GetDsDataValue(rid, 11187, "2017181")
     
    Print(AcademyFilter,"AcademyFilter",rid, 8267)
    Print(FundingBasis,"FundingBasis",rid, 8267)
    
    If FundingBasis = "Place" Then
    
    exclude(rid, 8267) 
    
    Else
    
        If AcademyFilter = 17181 Or AcademyFilter = 17182 Or AcademyFilter = 17183 then
        
        Dim PPAY3to6Proportion73Census As Decimal = _engine.GetDsDataValue(rid, 86948)
        Dim PPAY3to6Proportion73Adj As Decimal = _engine.GetDsDataValue(rid, 86064)
        Dim PPAY3to6Proportion73AdjString As String = _engine.GetDsDataValue(rid, 86064)
        Dim PPAY3to6Proportion73CensusString As String = _engine.GetDsDataValue(rid, 86948)
        Dim LA_AV As Decimal = LAtoProv(_engine.GetLaDataValue(rid, 86861, Nothing))
      
        Print(LA_AV,"LA average",rid, 8267)
        Print(PPAY3to6Proportion73Census,"PPAY3to6Proportion73Census",rid, 8267)
        Print(PPAY3to6Proportion73Adj,"PPAY3to6Proportion73Adj",rid, 8267)
    
            If string.IsNullOrEmpty(PPAY3to6Proportion73CensusString) And String.IsNullOrEmpty(PPAY3to6Proportion73Adjstring) then
            
            Result = LA_AV
            
            Else
      
                If String.IsNullOrEmpty(PPAY3to6Proportion73Adjstring) then
                
                Result = PPAY3to6Proportion73Census  
                
                Else 
                
                Result = PPAY3to6Proportion73Adj
                
                End If
                
            End If
    
            Else 
            
            exclude(rid, 8267)
            
            End If
            
    End if
    
    
result = System.Math.Round(result, 6, MidpointRounding.AwayFromZero)
_engine._productResultCache.Add(__key, result)
Return result
    
End Function 



Public Function GetProductResult_8268(rid As String) As Decimal
Dim __key = "8268" + rid
If (_engine._productResultCache.ContainsKey(__key)) = True Then Return _engine._productResultCache(__key)

    Dim result = 0
    
    Dim AcademyFilter As Decimal = GetProductResult_7582(rid) 
    Dim FundingBasis As String = _engine.GetDsDataValue(rid, 11187, "2017181")
     
    Print(AcademyFilter,"AcademyFilter",rid, 8268)
    Print(FundingBasis,"FundingBasis",rid, 8268)
    
    If FundingBasis = "Place" Then
    
    exclude(rid, 8268) 
    
    Else
    
        If AcademyFilter = 17181 Or AcademyFilter = 17182 Or AcademyFilter = 17183 then
        
        Dim PPAY3to6Proportion78Census As Decimal = _engine.GetDsDataValue(rid, 86950)
        Dim PPAY3to6Proportion78Adj As Decimal = _engine.GetDsDataValue(rid, 86066)
        Dim PPAY3to6Proportion78AdjString As String = _engine.GetDsDataValue(rid, 86066)
        Dim PPAY3to6Proportion78CensusString As String = _engine.GetDsDataValue(rid, 86950)
        Dim LA_AV As Decimal = LAtoProv(_engine.GetLaDataValue(rid, 86863, Nothing))
      
           Print(LA_AV,"LA average",rid, 8268)
           Print(PPAY3to6Proportion78Census,"PPAY3to6Proportion78Census",rid, 8268)
           Print(PPAY3to6Proportion78Adj,"PPAY3to6Proportion78Adj",rid, 8268)
      
               If String.IsNullOrEmpty(PPAY3to6Proportion78CensusString) And String.IsNullOrEmpty(PPAY3to6Proportion78Adjstring) then
               
               Result = LA_AV
               
               Else
      
                If String.IsNullOrEmpty(PPAY3to6Proportion78Adjstring) then
                
                Result = PPAY3to6Proportion78Census  
                
                Else 
                
                Result = PPAY3to6Proportion78Adj
                
                End If
                
            End if
    
            Else 
            
            exclude(rid, 8268)
            
            End If
            
    End if
    
    
result = System.Math.Round(result, 6, MidpointRounding.AwayFromZero)
_engine._productResultCache.Add(__key, result)
Return result
    
    
End Function 
  


Public Function GetProductResult_12255(rid As String) As Decimal
 Dim result As Decimal = 0
 
Dim __key = "12255" + rid
If (_engine._productResultCache.ContainsKey(__key)) = True Then Return _engine._productResultCache(__key)

 Dim EstNOR_Y5 As Decimal = _engine.GetDsDataValue(rid, 87282)
 Dim EstNOR_Y6 As Decimal = _engine.GetDsDataValue(rid, 87284)
 
 Print(EstNOR_Y5,"Year 5",rid, 12255)
 Print(EstNOR_Y6,"Year 6",rid, 12255)
 
 
 Result = (EstNOR_Y5 + EstNOR_Y6) 
 
 
_engine._productResultCache.Add(__key, result)
Return result
 
End Function


Public Function GetProductResult_7648(rid As String) As Decimal
Dim __key = "7648" + rid
If (_engine._productResultCache.ContainsKey(__key)) = True Then Return _engine._productResultCache(__key)

 Dim result = 0
 
 
 Dim DateOpened as date = _engine.GetDsDataValue(rid, 9077)
 Dim IsNull As Boolean
 IsNull = IIf(_engine.GetApprovedDsDataValue(rid, 86002), false, true) 
 Dim NOR_Inp_Adj As Decimal = _engine.GetDsDataValue(rid, 86002) 
 Dim NOR_Y5_Y6_Cen As Decimal = _engine.GetDsDataValue(rid, 87322)
 Dim NOR_Y5_Y6_Est As Decimal = GetProductResult_12255(rid) 
 Dim NOR_Y5_Y6_APT As Decimal = _engine.GetDsDataValue(rid, 86276)
 Dim AcadFilter As Decimal = GetProductResult_7582(rid) 
 Dim FundingBasis as string = GetProductResult_7594(rid)  
 Dim NOR_Y5_Y6 As Decimal
 Dim Guaranteed_Numbers As string = _engine.GetDsDataValue(rid, 88442)
 Dim InpAdj_NOR As Decimal = _engine.GetDsDataValue(rid, 85996)
 Dim RFDC_NOR As Decimal = GetProductResult_10460(rid) 
 
 Print(IsNull, "Input Adj Null check", rid, 7648)
 Print(NOR_Inp_Adj, "NOR InputsAdj", rid, 7648)
 Print(NOR_Y5_Y6_Cen,"NOR_Y5_Y6 Census",rid, 7648)
 Print(NOR_Y5_Y6_Est,"NOR_Y5_Y6 Estimate",rid, 7648)
 Print(FundingBasis,"Funding Basis",rid, 7648)
 Print(Guaranteed_Numbers, "Guaranteed_Numbers",rid, 7648)
 Print(InpAdj_NOR, "InpAdj_NOR",rid, 7648)
 Print(RFDC_NOR, "RFDC_NOR",rid, 7648)
 
If currentscenario.periodid = 2017181 And AcadFilter = 17181 then 
 If FundingBasis = 1 And IsNull = False Then
 NOR_Y5_Y6 = NOR_Inp_Adj
 ElseIf FundingBasis = 1 And IsNull = True Then
 NOR_Y5_Y6 = NOR_Y5_Y6_Cen 
 ElseIf (FundingBasis = 2 And Guaranteed_Numbers = "Yes" And InpAdj_NOR > RFDC_NOR) Then
 NOR_Y5_Y6 = NOR_Inp_Adj
 ElseIf FundingBasis = 2 Then
 NOR_Y5_Y6 = NOR_Y5_Y6_Est 
 End if 
 ElseIf currentscenario.periodid = 2017181 And (AcadFilter = 17182 Or AcadFilter = 17183) Then
 If FundingBasis = 1 Then
 NOR_Y5_Y6 = NOR_Y5_Y6_APT
 ElseIf (FundingBasis = 2 And Guaranteed_Numbers = "Yes" And InpAdj_NOR > RFDC_NOR) Then
 NOR_Y5_Y6 = NOR_Inp_Adj
 ElseIf FundingBasis = 2 Then
 NOR_Y5_Y6 = NOR_Y5_Y6_Est
 End If 
 Else Exclude(rid, 7648) 
End If





 
 result = NOR_Y5_Y6
 
_engine._productResultCache.Add(__key, result)
Return result
 
End Function

Public Function GetProductResult_8271(rid As String) As Decimal
Dim __key = "8271" + rid
If (_engine._productResultCache.ContainsKey(__key)) = True Then Return _engine._productResultCache(__key)

    Dim result = 0
    
    Dim AcademyFilter As Decimal = GetProductResult_7582(rid) 
    Dim FundingBasis As String = _engine.GetDsDataValue(rid, 11187, "2017181")
     
    Print(AcademyFilter,"AcademyFilter",rid, 8271)
    Print(FundingBasis,"FundingBasis",rid, 8271)
    
    If FundingBasis = "Place" Then
    
    exclude(rid, 8271) 
    
    Else
    
        If AcademyFilter = 17181 Or AcademyFilter = 17182 Or AcademyFilter = 17183 then
        
        Dim PPAY3to6NOR As Decimal = GetProductResult_7648(rid) 
        
        Result = PPAY3to6NOR
        
        else
        
        exclude(rid, 8271)
        
        End if
        
    End if
    
    
_engine._productResultCache.Add(__key, result)
Return result
    
End Function


Public Function GetProductResult_8276(rid As String) As Decimal
Dim __key = "8276" + rid
If (_engine._productResultCache.ContainsKey(__key)) = True Then Return _engine._productResultCache(__key)

    Dim result = 0
    
    Dim AcademyFilter As Decimal = GetProductResult_7582(rid) 
   Dim FundingBasis As String = _engine.GetDsDataValue(rid, 11187, "2017181")
     
    Print(AcademyFilter,"AcademyFilter",rid, 8276)
    Print(FundingBasis,"FundingBasis",rid, 8276)
    
    If FundingBasis = "Place" Then
    
    exclude(rid, 8276) 
    
    Else
    
        If AcademyFilter = 17181 Or AcademyFilter = 17182 Or AcademyFilter = 17183 then
        
        Dim PPAindicator As String = GetProductResult_8266(rid) 
        Dim PPAY3to6Proportion73 As Decimal = GetProductResult_8267(rid) 
        Dim PPAY3to6Proportion78 As Decimal = GetProductResult_8268(rid) 
        Dim PPAY3to6NOR As Decimal = GetProductResult_8271(rid) 
        Dim PPAY3to6NotAchieving73 As Decimal = PPAY3to6Proportion73 * PPAY3to6NOR
        Dim PPAY3to6NotAchieving78 As Decimal = PPAY3to6Proportion78 * PPAY3to6NOR
    
        Print(PPAindicator,"P120_PPAindicator",rid, 8276)
        Print(PPAY3to6Proportion73,"P121_ PPAY3to6Proportion73",rid, 8276)
        Print(PPAY3to6Proportion78,"P122_PPAY3to6Proportion78",rid, 8276)
        Print(PPAY3to6NOR,"P124_PPAY3to6NOR",rid, 8276)
    
            If PPAindicator = 0 then
        
            result = 0 
            
            End if
    
                If PPAindicator = 73 then
                
                result  = PPAY3to6NotAchieving73
                
                End if
    
                    If PPAindicator = 78 then
                    
                    result =  PPAY3to6NotAchieving78
                    
                    End If
    
                    Else
                    
                    exclude(rid, 8276)
                    
                    End if
    End if
    
    
result = System.Math.Round(result, 6, MidpointRounding.AwayFromZero)
_engine._productResultCache.Add(__key, result)
Return result
    
End Function
   


Public Function GetProductResult_8278(rid As String) As Decimal
Dim __key = "8278" + rid
If (_engine._productResultCache.ContainsKey(__key)) = True Then Return _engine._productResultCache(__key)

    Dim result = 0
    
    Dim AcademyFilter As Decimal = GetProductResult_7582(rid) 
    Dim FundingBasis As String = _engine.GetDsDataValue(rid, 11187, "2017181")
     
    Print(AcademyFilter,"AcademyFilter",rid, 8278)
    Print(FundingBasis,"FundingBasis",rid, 8278)
    
    If FundingBasis = "Place" Then
    
    exclude(rid, 8278) 
    
    Else
    
        If AcademyFilter = 17181 Or AcademyFilter = 17182 Or AcademyFilter = 17183 then
        
        Dim PPAPupilsY1to2NotAchieving As Decimal = GetProductResult_8277(rid) 
        Dim PPAPupilsY3to6NotAchieving As Decimal = GetProductResult_8276(rid) 
        Dim PPATotalPupilsY1to6NotAchieving As Decimal = PPAPupilsY1to2NotAchieving + PPAPupilsY3to6NotAchieving 

        Print(PPAPupilsY1to2NotAchieving,"P130_PPAPupilsY1to2NotAchieving",rid, 8278)
        Print(PPAPupilsY3to6NotAchieving,"P129_PPAPupilsY3to6NotAchieving",rid, 8278)
        Print(PPATotalPupilsY1to6NotAchieving,"P131_PPATotalPupilsY1to6NotAchieving",rid, 8278)

        Result = PPATotalPupilsY1to6NotAchieving
    
        else
        
        exclude(rid, 8278)
        
        End if
        
    End if
    
    
result = System.Math.Round(result, 6, MidpointRounding.AwayFromZero)
_engine._productResultCache.Add(__key, result)
Return result
    
End Function
      


Public Function GetProductResult_8279(rid As String) As Decimal
Dim __key = "8279" + rid
If (_engine._productResultCache.ContainsKey(__key)) = True Then Return _engine._productResultCache(__key)

 Dim result = 0
 
 Dim AcademyFilter As Decimal = GetProductResult_7582(rid) 
 Dim FundingBasis As String = _engine.GetDsDataValue(rid, 11187, "2017181")
 
 Print(AcademyFilter,"AcademyFilter",rid, 8279)
 Print(FundingBasis,"FundingBasis",rid, 8279)
 
 If FundingBasis = "Place" then
 
 exclude(rid, 8279) 
 
 Else
 
 If AcademyFilter = 17181 Or AcademyFilter = 17182 Or AcademyFilter = 17183 then
 
 Dim PPATotalPupilsY1to6NotAchieving As Decimal = GetProductResult_8278(rid) 
 Dim PPAY1to6NOR As Decimal = GetProductResult_8272(rid)  + GetProductResult_8271(rid) 
 Dim PPATotalProportionNotAchieving As Decimal =Divide(PPATotalPupilsY1to6NotAchieving, PPAY1to6NOR)
 
 If PPAY1to6NOR = 0 OR PPATotalPupilsY1to6NotAchieving = 0 then

 Result = 0
 
 else
 
 Print(PPATotalPupilsY1to6NotAchieving,"P131_PPATotalPupilsY1to6NotAchieving",rid, 8279)
 Print(PPAY1to6NOR,"P125_PPAY1to2NORplusP124_PPAY3to6NOR",rid, 8279)
 Print(PPATotalProportionNotAchieving,"P132_PPATotalProportionNotAchieving",rid, 8279)
 
 Result = PPATotalProportionNotAchieving
 End if
 
 else
 
 exclude(rid, 8279)
 
 End if
 
 End if
 
 
result = System.Math.Round(result, 6, MidpointRounding.AwayFromZero)
_engine._productResultCache.Add(__key, result)
Return result
 
End Function


Public Function GetProductResult_8273(rid As String) As Decimal
Dim __key = "8273" + rid
If (_engine._productResultCache.ContainsKey(__key)) = True Then Return _engine._productResultCache(__key)

    Dim result = 0
    
    Dim AcademyFilter As Decimal = GetProductResult_7582(rid) 
    Dim FundingBasis As String = _engine.GetDsDataValue(rid, 11187, "2017181")
     
    Print(AcademyFilter,"AcademyFilter",rid, 8273)
    Print(FundingBasis,"FundingBasis",rid, 8273)
    
    If FundingBasis = "Place" Then
    
    exclude(rid, 8273)
    
    Else
    
        If AcademyFilter = 17181 Or AcademyFilter = 17182 Or AcademyFilter = 17183 then
        
        Dim PPAPriNOR As Decimal = GetProductResult_7667(rid) 
    
        Print(PPAPriNOR,"P126_PPAPriNOR",rid, 8273)
    
        Result = PPAPriNOR
        
        else
        
        exclude(rid, 8273)
        
        End if
        
    End if
    
    
_engine._productResultCache.Add(__key, result)
Return result
    
End Function


Public Function GetProductResult_8274(rid As String) As Decimal
Dim __key = "8274" + rid
If (_engine._productResultCache.ContainsKey(__key)) = True Then Return _engine._productResultCache(__key)

    Dim result = 0
    
    Dim AcademyFilter As Decimal = GetProductResult_7582(rid) 
    Dim FundingBasis As String = _engine.GetDsDataValue(rid, 11187, "2017181")
     
    Print(AcademyFilter,"AcademyFilter",rid, 8274)
    Print(FundingBasis,"FundingBasis",rid, 8274)
    
    If FundingBasis = "Place" Then
    
    exclude(rid, 8274) 
    
    Else
    
        If AcademyFilter = 17181 Or AcademyFilter = 17182 Or AcademyFilter = 17183 then
        
        Dim provider As String = _engine.GetDsDataValue(rid, 9070)
        Dim PPARate As Decimal = LaToProv(_engine.GetLaDataValue(rid, 86610, Nothing))
           
        Print(PPARate,"P127_PPARate",rid, 8274)
    
        Result = PPARate
        
        else
        
        exclude(rid, 8274)
        
        End if
        
    End if
    
    
result = System.Math.Round(result, 6, MidpointRounding.AwayFromZero)
_engine._productResultCache.Add(__key, result)
Return result

End Function


Public Function GetProductResult_8280(rid As String) As Decimal
Dim __key = "8280" + rid
If (_engine._productResultCache.ContainsKey(__key)) = True Then Return _engine._productResultCache(__key)

    Dim result = 0
    
    Dim AcademyFilter As Decimal = GetProductResult_7582(rid) 
    Dim FundingBasis As String = _engine.GetDsDataValue(rid, 11187, "2017181")
   
     
    Print(AcademyFilter,"AcademyFilter",rid, 8280)
    Print(FundingBasis,"FundingBasis",rid, 8280)
    
    If FundingBasis = "Place" Then
    
    exclude(rid, 8280) 
    
    Else

       
        
       
                
           
            
           
          
            End if
            
            print(FundingBasis, "FundingBasisType",rid, 8280)
            print(AcademyFilter, "AcademyFilter", rid, 8280)
        
                If AcademyFilter = 17181 Or (AcademyFilter = 17182 And FundingBasis = "Estimate") Or (AcademyFilter = 17183 And FundingBasis = "Estimate") then
    
                Dim P132_PPATotalProportionNotAchieving As Decimal = GetProductResult_8279(rid) 
                Dim P126_PPAPriNOR As Decimal = GetProductResult_8273(rid) 
                Dim P127_PPARate As Decimal =   GetProductResult_8274(rid) 
                Dim P133_PPATotal_Funding  As Decimal =  P126_PPAPriNOR *  P127_PPARate *  P132_PPATotalProportionNotAchieving
        
                Print(P132_PPATotalProportionNotAchieving,"P132_PPATotalProportionNotAchieving",rid, 8280)
                Print(P126_PPAPriNOR,"P126_PPAPriNOR",rid, 8280)
                Print(P127_PPARate,"P127_PPARate",rid, 8280)
     
                result = P133_PPATotal_Funding
                
                Else 
    
                    If (AcademyFilter = 17182 And FundingBasis = "Census") Or (AcademyFilter = 17183 And FundingBasis = "Census") then
                    
                    Dim P133_PPATotal_FundingAPT As Decimal = _engine.GetDsDataValue(rid, 86136)
    
                    Print(P133_PPATotal_FundingAPT,"P133_PPATotal_FundingAPT",rid, 8280)
    
                    Result = P133_PPATotal_FundingAPT
                    
                    Else
                    
                    Exclude(rid, 8280)
                    
                    End if
  
                End if
       
    
        
result = System.Math.Round(result, 2, MidpointRounding.AwayFromZero)
_engine._productResultCache.Add(__key, result)
Return result
        
End Function
         


Public Function GetProductResult_8283(rid As String) As Decimal
Dim __key = "8283" + rid
If (_engine._productResultCache.ContainsKey(__key)) = True Then Return _engine._productResultCache(__key)

    Dim result = 0
    
    Dim AcademyFilter As Decimal = GetProductResult_7582(rid) 
    Dim FundingBasis As String = _engine.GetDsDataValue(rid, 11187, "2017181")
     
    Print(AcademyFilter,"AcademyFilter",rid, 8283)
    Print(FundingBasis,"FundingBasis",rid, 8283)
    
    If FundingBasis = "Place" Then
    
    exclude(rid, 8283)
    
    Else

        If AcademyFilter = 17181 Or AcademyFilter = 17182 Or AcademyFilter = 17183 then
      
        Dim P133_PPATotalFunding As Decimal = GetProductResult_8280(rid) 
        Dim Days_Open As Decimal = GetProductResult_8151(rid) 
        Dim Year_Days As Decimal = 365                     

        Print(P133_PPATotalFunding,"P133 PPATotalFunding",rid, 8283)
        Print(Days_Open,"Days Open",rid, 8283)
        Print(Year_Days,"Year Days",rid, 8283)
    
        Result = (P133_PPATotalFunding) *Divide(Days_Open, Year_Days)
    
        Else
        
        Exclude(rid, 8283)  
        
        End If   
        
    End if   
      
    
result = System.Math.Round(result, 6, MidpointRounding.AwayFromZero)
_engine._productResultCache.Add(__key, result)
Return result
    
End Function


Public Function GetProductResult_12265(rid As String) As Decimal
 Dim result As Decimal = 0
 
Dim __key = "12265" + rid
If (_engine._productResultCache.ContainsKey(__key)) = True Then Return _engine._productResultCache(__key)

 Dim DateOpened as date = _engine.GetDsDataValue(rid, 9077)
 Dim IsNull As Boolean
 IsNull = IIf(_engine.GetApprovedDsDataValue(rid, 88812), false, true) 
 Dim NOR_Y7_Inp_Adj As Decimal = _engine.GetDsDataValue(rid, 88812)
 Dim NOR_Y7_Cen As Decimal = _engine.GetDsDataValue(rid, 87348)
 Dim NOR_Y7_Est As Decimal = _engine.GetDsDataValue(rid, 87286)
 Dim NOR_Y7_APT As Decimal = _engine.GetDsDataValue(rid, 88826)
 Dim AcadFilter As Decimal = GetProductResult_7582(rid) 
 Dim FundingBasis as string = GetProductResult_7594(rid) 
 Dim NOR_Y7 As Decimal
 Dim Guaranteed_Numbers As string = _engine.GetDsDataValue(rid, 88442)
 Dim InpAdj_NOR As Decimal = _engine.GetDsDataValue(rid, 85996)
 Dim RFDC_NOR As Decimal = GetProductResult_10460(rid) 
 

 
 Print(IsNull, "Input Adj Null check", rid, 12265)
 Print(NOR_Y7_Inp_Adj, "NOR Y7 InputsAdj", rid, 12265)
 Print(NOR_Y7_Cen,"NORY7 Census",rid, 12265)
 Print(NOR_Y7_Est,"NORY7 Estimate",rid, 12265)
 Print(FundingBasis,"Funding Basis",rid, 12265)
 Print(Guaranteed_Numbers, "Guaranteed_Numbers",rid, 12265)
 Print(InpAdj_NOR, "InpAdj_NOR",rid, 12265)
 Print(RFDC_NOR, "RFDC_NOR",rid, 12265)
 
 
 
 If currentscenario.periodid = 2017181 And (AcadFilter = 17181 Or AcadFilter = 17182 Or AcadFilter = 17183) Then
 If FundingBasis = 1 And IsNull = False Then
 NOR_Y7 = NOR_Y7_Inp_Adj
 ElseIf FundingBasis = 1 And IsNull = True Then
 NOR_Y7 = NOR_Y7_Cen 
 ElseIf (FundingBasis = 2 And Guaranteed_Numbers = "Yes" And InpAdj_NOR > RFDC_NOR) Then
 NOR_Y7 = NOR_Y7_Inp_Adj
 ElseIf FundingBasis = 2 Then
 NOR_Y7 = NOR_Y7_Est 
 End If 
 Else Exclude(rid, 12265) 
End If

 result = NOR_Y7
 
 
_engine._productResultCache.Add(__key, result)
Return result
End Function


Public Function GetProductResult_12256(rid As String) As Decimal
 Dim result As Decimal = 0
 
Dim __key = "12256" + rid
If (_engine._productResultCache.ContainsKey(__key)) = True Then Return _engine._productResultCache(__key)

 Dim EstNOR_Y8 As Decimal = _engine.GetDsDataValue(rid, 87288)
 Dim EstNOR_Y9 As Decimal = _engine.GetDsDataValue(rid, 87290)
 Dim EstNOR_Y10 As Decimal = _engine.GetDsDataValue(rid, 87292)
 Dim EstNOR_Y11 As Decimal = _engine.GetDsDataValue(rid, 87294)
 
 Print(EstNOR_Y8,"Year 8",rid, 12256)
 Print(EstNOR_Y9,"Year 9",rid, 12256)
 Print(EstNOR_Y10,"Year 10",rid, 12256)
 Print(EstNOR_Y11,"Year 11",rid, 12256)
 
 Result = (EstNOR_Y8 + EstNOR_Y9 + EstNOR_Y10 + EstNOR_Y11)
 
 
 
_engine._productResultCache.Add(__key, result)
Return result
End Function


Public Function GetProductResult_12269(rid As String) As Decimal
 Dim result As Decimal = 0
 
Dim __key = "12269" + rid
If (_engine._productResultCache.ContainsKey(__key)) = True Then Return _engine._productResultCache(__key)

 Dim DateOpened as date = _engine.GetDsDataValue(rid, 9077)
 Dim IsNull As Boolean
 IsNull = IIf(_engine.GetApprovedDsDataValue(rid, 88813), false, true) 
 Dim NOR_Y8to11_Inp_Adj As Decimal = _engine.GetDsDataValue(rid, 88813)
 Dim NOR_Y8to11_Cen As Decimal = _engine.GetDsDataValue(rid, 88855)
 Dim NOR_Y8to11_Est As Decimal = GetProductResult_12256(rid) 
 Dim NOR_Y8to11_APT As Decimal = _engine.GetDsDataValue(rid, 88827)
 Dim AcadFilter As Decimal = GetProductResult_7582(rid) 
 Dim FundingBasis as string = GetProductResult_7594(rid) 
 Dim NOR_Y8to11 As Decimal
 Dim Guaranteed_Numbers As string = _engine.GetDsDataValue(rid, 88442)
 Dim InpAdj_NOR As Decimal = _engine.GetDsDataValue(rid, 85996)
 Dim RFDC_NOR As Decimal = GetProductResult_10460(rid) 
 

 
 Print(IsNull, "Input Adj Null check", rid, 12269)
 Print(NOR_Y8to11_Inp_Adj, "NOR Y8to11 InputsAdj", rid, 12269)
 Print(NOR_Y8to11_Cen,"NORY8to11 Census",rid, 12269)
 Print(NOR_Y8to11_Est,"NORY8to11 Estimate",rid, 12269)
 Print(FundingBasis,"Funding Basis",rid, 12269)
 Print(Guaranteed_Numbers, "Guaranteed_Numbers",rid, 12269)
 Print(InpAdj_NOR, "InpAdj_NOR",rid, 12269)
 Print(RFDC_NOR, "RFDC_NOR",rid, 12269)
 
 
 
 If currentscenario.periodid = 2017181 And (AcadFilter = 17181 Or AcadFilter = 17182 Or AcadFilter = 17183) Then
 If FundingBasis = 1 And IsNull = False Then
 NOR_Y8to11 = NOR_Y8to11_Inp_Adj
 ElseIf FundingBasis = 1 And IsNull = True Then
 NOR_Y8to11 = NOR_Y8to11_Cen 
 ElseIf (FundingBasis = 2 And Guaranteed_Numbers = "Yes" And InpAdj_NOR > RFDC_NOR) Then
 NOR_Y8to11 = NOR_Y8to11_Inp_Adj
 ElseIf FundingBasis = 2 Then
 NOR_Y8to11 = NOR_Y8to11_Est 
 End If 
 Else Exclude(rid, 12269) 
End If

 result = NOR_Y8to11 
 
 
 
_engine._productResultCache.Add(__key, result)
Return result
End Function


Public Function GetProductResult_8284(rid As String) As Decimal
Dim __key = "8284" + rid
If (_engine._productResultCache.ContainsKey(__key)) = True Then Return _engine._productResultCache(__key)

    Dim result = 0
    
    Dim AcademyFilter As Decimal = GetProductResult_7582(rid) 
    Dim FundingBasis As String = _engine.GetDsDataValue(rid, 11187, "2017181")
     
    Print(AcademyFilter,"AcademyFilter",rid, 8284)
    Print(FundingBasis,"FundingBasis",rid, 8284)
    
    If FundingBasis = "Place" Then
    
    exclude(rid, 8284)
    
    Else
         
        If AcademyFilter = 17181 Or AcademyFilter = 17182 Or AcademyFilter = 17183 then
        
        Dim SecPAFactorCensus As Decimal = _engine.GetDsDataValue(rid, 88846)
        Dim SecPAFactorAdj As Decimal = _engine.GetDsDataValue(rid, 88816)
        Dim SecPAFactorAdjString As String = _engine.GetDsDataValue(rid, 88816)
        Dim SecPAFactorCensusString As String = _engine.GetDsDataValue(rid, 88846)
        Dim LA_AV As Decimal = LAtoProv(_engine.GetLaDataValue(rid, 88852, Nothing))
    
        Print(LA_AV,"LA average",rid, 8284)
        Print(SecPAFactorCensus,"SecPAFactorCensus",rid, 8284)
        Print(SecPAFactorAdj,"SecPAFactorAdj",rid, 8284)
       
            If string.IsNullOrEmpty(SecPAFactorCensusString) And string.IsNullOrEmpty(SecPAFactorAdjString) then
            
            Result = LA_AV
            
            Else

                If string.IsNullOrEmpty(SecPAFactorAdjString) then
                
                Result = SecPAFactorCensus
                
                Else
                
                Result = SecPAFactorAdj
                
                End if
            
            End if
    
            
            Else
            
            Exclude(rid, 8284)
            
            End if
        
        End if
    
    
result = System.Math.Round(result, 6, MidpointRounding.AwayFromZero)
_engine._productResultCache.Add(__key, result)
Return result
    
End Function
         


Public Function GetProductResult_12267(rid As String) As Decimal
 Dim result As Decimal = 0
 
Dim __key = "12267" + rid
If (_engine._productResultCache.ContainsKey(__key)) = True Then Return _engine._productResultCache(__key)

 Dim AcademyFilter As Decimal = GetProductResult_7582(rid) 
    Dim FundingBasis As String = _engine.GetDsDataValue(rid, 11187, "2017181")
     
    Print(AcademyFilter,"AcademyFilter",rid, 12267)
    Print(FundingBasis,"FundingBasis",rid, 12267)
    
    If FundingBasis = "Place" Then
    
    exclude(rid, 12267)
    
    Else
         
        If AcademyFilter = 17181 Or AcademyFilter = 17182 Or AcademyFilter = 17183 then

 Dim NatWeight As Decimal = LAtoProv(_engine.GetLaDataValue(rid, 88841, Nothing))

 Result = NatWeight
 
 Else exclude(rid, 12267)
 
 End If
 
 End If
 
 
result = System.Math.Round(result, 6, MidpointRounding.AwayFromZero)
_engine._productResultCache.Add(__key, result)
Return result
End Function


Public Function GetProductResult_8285(rid As String) As Decimal
Dim __key = "8285" + rid
If (_engine._productResultCache.ContainsKey(__key)) = True Then Return _engine._productResultCache(__key)

    Dim result = 0
    
    Dim AcademyFilter As Decimal = GetProductResult_7582(rid) 
    Dim FundingBasis As String = _engine.GetDsDataValue(rid, 11187, "2017181")
     
    Print(AcademyFilter,"AcademyFilter",rid, 8285)
    Print(FundingBasis,"FundingBasis",rid, 8285)
    
    If FundingBasis = "Place" Then
    
    exclude(rid, 8285)
    
    Else
         
        If AcademyFilter = 17181 Or AcademyFilter = 17182 Or AcademyFilter = 17183 then
        
        Dim SecPAFactorCensus As Decimal = _engine.GetDsDataValue(rid, 88847)
        Dim SecPAFactorAdj As Decimal = _engine.GetDsDataValue(rid, 88817)
        Dim SecPAFactorAdjString As String = _engine.GetDsDataValue(rid, 88817)
        Dim SecPAFactorCensusString As String = _engine.GetDsDataValue(rid, 88847)
        Dim LA_AV As Decimal = LAtoProv(_engine.GetLaDataValue(rid, 88853, Nothing))
    
        Print(LA_AV,"LA average",rid, 8285)
        Print(SecPAFactorCensus,"SecPAFactorCensus",rid, 8285)
        Print(SecPAFactorAdj,"SecPAFactorAdj",rid, 8285)
       
            If string.IsNullOrEmpty(SecPAFactorCensusString) And string.IsNullOrEmpty(SecPAFactorAdjString) then
            
            Result = LA_AV
            
            Else

                If string.IsNullOrEmpty(SecPAFactorAdjString) then
                
                Result = SecPAFactorCensus
                
                Else
                
                Result = SecPAFactorAdj
                
                End if
            
            End if
    
            
            Else
            
            Exclude(rid, 8285)
            
            End if
        
        End if
    
    
result = System.Math.Round(result, 6, MidpointRounding.AwayFromZero)
_engine._productResultCache.Add(__key, result)
Return result
    
End Function


Public Function GetProductResult_12268(rid As String) As Decimal
 Dim result As Decimal = 0
 
Dim __key = "12268" + rid
If (_engine._productResultCache.ContainsKey(__key)) = True Then Return _engine._productResultCache(__key)

 Dim AcademyFilter As Decimal = GetProductResult_7582(rid) 
    Dim FundingBasis As String = _engine.GetDsDataValue(rid, 11187, "2017181")
     
    Print(AcademyFilter,"AcademyFilter",rid, 12268)
    Print(FundingBasis,"FundingBasis",rid, 12268)
    
    If FundingBasis = "Place" Then
    
    exclude(rid, 12268)
    
    Else
         
        If AcademyFilter = 17181 Or AcademyFilter = 17182 Or AcademyFilter = 17183 then
        
        Dim Y7NOR As Decimal = GetProductResult_12265(rid) 
 Dim Y8to11 As Decimal = GetProductResult_12269(rid) 
 Dim Y7Factor As Decimal = GetProductResult_8284(rid) 
 Dim NatWeight As Decimal = GetProductResult_12267(rid) 
 Dim Y8to11Factor As Decimal = GetProductResult_8285(rid) 
 
 Print(Y7NOR, "Y7 NOR", rid, 12268)
 Print(Y8to11, "Y8 To 11 NOR", rid, 12268)
 Print(Y7Factor, "Y7 Factor", rid, 12268)
 Print(NatWeight, "Nat Weighting", rid, 12268)
 Print(Y8to11Factor, "Y8 To 11 Factor", rid, 12268)
 
 Result = Divide(((Y7Factor * Y7NOR * NatWeight) + (Y8to11 * Y8to11Factor)), (Y7NOR + Y8to11))
 
 
    
            
            End if
        
        End if
 
 
 
 
 
result = System.Math.Round(result, 6, MidpointRounding.AwayFromZero)
_engine._productResultCache.Add(__key, result)
Return result
End Function


Public Function GetProductResult_8286(rid As String) As Decimal
Dim __key = "8286" + rid
If (_engine._productResultCache.ContainsKey(__key)) = True Then Return _engine._productResultCache(__key)

    Dim result = 0
    
    Dim AcademyFilter As Decimal = GetProductResult_7582(rid) 
    Dim FundingBasis As String = _engine.GetDsDataValue(rid, 11187, "2017181")
     
    Print(AcademyFilter,"AcademyFilter",rid, 8286)
    Print(FundingBasis,"FundingBasis",rid, 8286)
    
    If FundingBasis = "Place" Then
    
    exclude(rid, 8286) 
    
    Else 
     
        If AcademyFilter = 17181 Or AcademyFilter = 17182 Or AcademyFilter = 17183 then
        
        Dim provider As String = _engine.GetDsDataValue(rid, 9070)
        Dim SecPARate As Decimal = LaToProv(_engine.GetLaDataValue(rid, 86622, Nothing))
       
        Print(SecPARate,"P138_SecPARate",rid, 8286)
    
        Result = SecPARate
        
        else
        
        exclude(rid, 8286)
        
        End If
        
    End if
    
    
result = System.Math.Round(result, 6, MidpointRounding.AwayFromZero)
_engine._productResultCache.Add(__key, result)
Return result
    
End Function


Public Function GetProductResult_8287(rid As String) As Decimal
Dim __key = "8287" + rid
If (_engine._productResultCache.ContainsKey(__key)) = True Then Return _engine._productResultCache(__key)

    Dim result = 0
    
    Dim AcademyFilter As Decimal = GetProductResult_7582(rid) 
    Dim FundingBasis As String = _engine.GetDsDataValue(rid, 11187, "2017181")
                
     
    Print(AcademyFilter,"AcademyFilter",rid, 8287)
    Print(FundingBasis,"FundingBasis",rid, 8287)
    
    If FundingBasis = "Place" Then
    
    exclude(rid, 8287) 
    
    Else

       
        
       
                
           
            
           
          
            End if
            
            print(FundingBasis, "FundingBasisType",rid, 8287)         
            print(AcademyFilter, "AcademyFilter", rid, 8287)
        
                If AcademyFilter = 17181 Or (AcademyFilter = 17182 And FundingBasis = "Estimate") Or (AcademyFilter = 17183 And FundingBasis = "Estimate") then
            
                Dim P136_SecPAFactor As Decimal = GetProductResult_12268(rid) 
                Dim P137_SecPAPupils As Decimal = GetProductResult_7673(rid) 
                Dim P138_SecPARate As Decimal = GetProductResult_8286(rid) 
                Dim P139_SecPASubtotal As Decimal = P136_SecPAFactor * P137_SecPAPupils * P138_SecPARate
        
                Print(P136_SecPAFactor,"P136_SecPAFactor",rid, 8287)
                Print(P137_SecPAPupils,"P137_SecPAPupils",rid, 8287)
                Print(P138_SecPARate,"P138_SecPARate",rid, 8287)
    
                Result = P139_SecPASubtotal
                
                else
        
                    If (AcademyFilter = 17182 And FundingBasis = "Census") Or (AcademyFilter = 17183 And FundingBasis = "Census") then
                    
                    Dim P139_SecPASubtotalAPT As Decimal = _engine.GetDsDataValue(rid, 86138)
                    
                    Print(P139_SecPASubtotalAPT,"P139_SecPASubtotalAPT",rid, 8287)
                    
                    Result = P139_SecPASubtotalAPT
                    
                    Else
                    
                    exclude(rid, 8287)
                    
                    End if
                
                End if
        
       
        
        
result = System.Math.Round(result, 2, MidpointRounding.AwayFromZero)
_engine._productResultCache.Add(__key, result)
Return result
    
End Function




Public Function GetProductResult_8290(rid As String) As Decimal
Dim __key = "8290" + rid
If (_engine._productResultCache.ContainsKey(__key)) = True Then Return _engine._productResultCache(__key)

    Dim result = 0
    
    Dim AcademyFilter As Decimal = GetProductResult_7582(rid) 
    Dim FundingBasis As String = _engine.GetDsDataValue(rid, 11187, "2017181")
     
    Print(AcademyFilter,"AcademyFilter",rid, 8290)
    Print(FundingBasis,"FundingBasis",rid, 8290)
    
    If FundingBasis = "Place" Then
    
    exclude(rid, 8290) 
    
    Else

        If AcademyFilter = 17181 Or AcademyFilter = 17182 Or AcademyFilter = 17183 then
      
        Dim P139_SecPASubtotal As Decimal = GetProductResult_8287(rid) 
        Dim Days_Open As Decimal = GetProductResult_8151(rid) 
        Dim Year_Days As Decimal = 365                     

        Print(P139_SecPASubtotal,"P139 SecPASubtotal",rid, 8290)
        Print(Days_Open,"Days Open",rid, 8290)
        Print(Year_Days,"Year Days",rid, 8290)
    
        Result = (P139_SecPASubtotal) *Divide(Days_Open, Year_Days)
    
        Else
        
        Exclude(rid, 8290)  
        
        End If             
    
    End if   
      
    
result = System.Math.Round(result, 6, MidpointRounding.AwayFromZero)
_engine._productResultCache.Add(__key, result)
Return result
    
End Function
   


Public Function GetProductResult_8292(rid As String) As Decimal
Dim __key = "8292" + rid
If (_engine._productResultCache.ContainsKey(__key)) = True Then Return _engine._productResultCache(__key)

 Dim result = 0
 
 Dim AcademyFilter As Decimal = GetProductResult_7582(rid) 
 Dim FundingBasis As String = _engine.GetDsDataValue(rid, 11187, "2017181")
 
 Print(AcademyFilter,"AcademyFilter",rid, 8292)
 Print(FundingBasis,"FundingBasis",rid, 8292)
 
 If FundingBasis = "Place" Then
 
 exclude(rid, 8292) 
 
 Else
 
 If AcademyFilter = 17181 Or AcademyFilter = 17182 Or AcademyFilter = 17183 then
 
 Dim ProviderName As String = _engine.GetDsDataValue(rid, 9070)
 Dim P144_EAL1PriRate As Decimal = LaToProv(_engine.GetLaDataValue(rid, 86570, Nothing)) 
 Dim LAMethod as string = LaToProv(_engine.GetLaDataValue(rid, 86568, Nothing))
 
 If LAMethod = "EAL 1 Primary" THEN
 
 Print(ProviderName,"Provider Name",rid, 8292)
 Print(P144_EAL1PriRate,"P144_EAL1PriRate",rid, 8292) 
 Print(LAMethod, "EAL Pri 1/EAL Pri 2/EAL Pri 3/NA", rid, 8292)
 
 Result = P144_EAL1PriRate
 
 Else
 
 Result = 0
 
 End if
 
 else
 
 exclude(rid, 8292)
 
 End if
 
 End if
 
 
result = System.Math.Round(result, 6, MidpointRounding.AwayFromZero)
_engine._productResultCache.Add(__key, result)
Return result
 
End Function


Public Function GetProductResult_8291(rid As String) As Decimal
Dim __key = "8291" + rid
If (_engine._productResultCache.ContainsKey(__key)) = True Then Return _engine._productResultCache(__key)

 Dim result = 0
 
 Dim AcademyFilter As Decimal = GetProductResult_7582(rid) 
 Dim FundingBasis As String = _engine.GetDsDataValue(rid, 11187, "2017181")
 
 Print(AcademyFilter,"AcademyFilter",rid, 8291)
 Print(FundingBasis,"FundingBasis",rid, 8291)
 
 If FundingBasis = "Place" Then
 
 exclude(rid, 8291) 
 
 Else
 
 If AcademyFilter = 17181 Or AcademyFilter = 17182 Or AcademyFilter = 17183 then
 
 Dim EAL1PriCensus As Decimal = _engine.GetDsDataValue(rid, 86932)
 Dim EAL1PriAdj As Decimal = _engine.GetDsDataValue(rid, 86048)
 Dim EAL1PriAdjString as string = _engine.GetDsDataValue(rid, 86048)
 Dim EAL1PriCensusString As String = _engine.GetDsDataValue(rid, 86932)
 Dim LA_AV As Decimal = latoprov(_engine.GetLaDataValue(rid, 86845, Nothing))
 
 Print(LA_AV,"LA average",rid, 8291)
 Print(EAL1PriCensus, "EAL 1 Pri Census", rid, 8291)
 Print(EAL1PriAdj, "EAL 1 Pri Adjusted", rid, 8291) 
 
 If string.IsNullOrEmpty(EAL1PriCensusString) And string.IsNullOrEmpty(EAL1PriAdjString) THEN
 
 Result = LA_AV
 
 ELSE
 
 If string.IsNullOrEmpty(EAL1PriAdjString) THEN
 
 Result = EAL1PriCensus
 
 Else
 
 Result = EAL1PriAdj
 
 End if
 
 End if
 
 ELSE
 
 Exclude(rid, 8291)
 
 End if
 
 End If
 
 
result = System.Math.Round(result, 6, MidpointRounding.AwayFromZero)
_engine._productResultCache.Add(__key, result)
Return result
 
End Function


Public Function GetProductResult_8293(rid As String) As Decimal
Dim __key = "8293" + rid
If (_engine._productResultCache.ContainsKey(__key)) = True Then Return _engine._productResultCache(__key)

 Dim result = 0
 
 Dim AcademyFilter As Decimal = GetProductResult_7582(rid) 
 Dim FundingBasis As String = _engine.GetDsDataValue(rid, 11187, "2017181")
 
 Print(AcademyFilter,"AcademyFilter",rid, 8293)
 Print(FundingBasis,"FundingBasis",rid, 8293)
 
 If FundingBasis = "Place" Then
 
 exclude(rid, 8293) 
 
 Else
 
 
 
 
 
 
 
 
 
 
 
 End if
 
 print(FundingBasis, "FundingBasisType",rid, 8293)
 print(AcademyFilter, "AcademyFilter", rid, 8293)
 
 If AcademyFilter = 17181 Or (AcademyFilter = 17182 And FundingBasis = "Estimate") Or (AcademyFilter = 17183 And FundingBasis = "Estimate") THEN
 
 Dim P143_EAL1PriPupils As Decimal = GetProductResult_7667(rid) 
 Dim P144_EAL1PriRate As Decimal = GetProductResult_8292(rid) 
 Dim P142_EAL1PriFactor As Decimal = GetProductResult_8291(rid)  
 Dim P145_EAL1PriSubtotal As Decimal = P143_EAL1PriPupils * P144_EAL1PriRate * P142_EAL1PriFactor
 
 Print(P143_EAL1PriPupils,"P143_EAL1PriPupils",rid, 8293)
 Print(P144_EAL1PriRate,"P144_EAL1PriRate",rid, 8293)
 Print(P142_EAL1PriFactor,"P145_EAL1PriFactor",rid, 8293) 
 
 Result = P145_EAL1PriSubtotal 
 
 else
 
 If (AcademyFilter = 17182 And FundingBasis = "Census") Or (AcademyFilter = 17183 And FundingBasis = "Census") then
 
 Dim EALSelectedbyLA As String = LaToProv(_engine.GetLaDataValue(rid, 86568, Nothing)) 
 
 Print(EALSelectedbyLA,"EALSelectedbyLA",rid, 8293)
 
 If EALSelectedbyLA = "EAL 1 Primary" then
 
 Dim P145_EAL1PriSubtotalAPT As Decimal = _engine.GetDsDataValue(rid, 86130) 
 
 Print(P145_EAL1PriSubtotalAPT,"P145_EAL1PriSubtotalAPT",rid, 8293)
 
 Result = P145_EAL1PriSubtotalAPT
 
 Else
 
 Result = 0
 
 End If
 
 Else
 
 exclude(rid, 8293)
 
 End If
 
 End if
 
 
 
 
result = System.Math.Round(result, 2, MidpointRounding.AwayFromZero)
_engine._productResultCache.Add(__key, result)
Return result
 
End Function


Public Function GetProductResult_8294(rid As String) As Decimal
Dim __key = "8294" + rid
If (_engine._productResultCache.ContainsKey(__key)) = True Then Return _engine._productResultCache(__key)

 Dim result = 0
 
 Dim AcademyFilter As Decimal = GetProductResult_7582(rid) 
 Dim FundingBasis As String = _engine.GetDsDataValue(rid, 11187, "2017181")
 
 Print(AcademyFilter,"AcademyFilter",rid, 8294)
 Print(FundingBasis,"FundingBasis",rid, 8294)
 
 If FundingBasis = "Place" Then
 
 exclude(rid, 8294) 
 
 Else
 
 If AcademyFilter = 17181 Or AcademyFilter = 17182 Or AcademyFilter = 17183 then
 
 Dim P145_EAL1PriSubtotal As Decimal = GetProductResult_8293(rid)  
 Dim Days_Open As Decimal = GetProductResult_8151(rid) 
 Dim Year_Days As Decimal = 365 
 
 Print(P145_EAL1PriSubtotal,"P145 EAL1PriSubtotal",rid, 8294)
 Print(Days_Open,"Days Open",rid, 8294)
 Print(Year_Days,"Year Days",rid, 8294)
 
 Result = (P145_EAL1PriSubtotal) *Divide(Days_Open, Year_Days)
 
 Else
 
 Exclude(rid, 8294) 
 
 End If 
 
 End If
 
 
result = System.Math.Round(result, 6, MidpointRounding.AwayFromZero)
_engine._productResultCache.Add(__key, result)
Return result
 
End Function


Public Function GetProductResult_8296(rid As String) As Decimal
Dim __key = "8296" + rid
If (_engine._productResultCache.ContainsKey(__key)) = True Then Return _engine._productResultCache(__key)

 Dim result = 0
 
 Dim AcademyFilter As Decimal = GetProductResult_7582(rid) 
 Dim FundingBasis As String = _engine.GetDsDataValue(rid, 11187, "2017181")
 
 Print(AcademyFilter,"AcademyFilter",rid, 8296)
 Print(FundingBasis,"FundingBasis",rid, 8296)
 
 If FundingBasis = "Place" Then
 
 exclude(rid, 8296) 
 
 Else
 
 If AcademyFilter = 17181 Or AcademyFilter = 17182 Or AcademyFilter = 17183 then
 
 Dim ProviderName As String = _engine.GetDsDataValue(rid, 9070)
 Dim P149_EAL2PriRate As Decimal = LaToProv(_engine.GetLaDataValue(rid, 86570, Nothing)) 
 Dim LAMethod as string = LaToProv(_engine.GetLaDataValue(rid, 86568, Nothing))
 
 If LAMethod = "EAL 2 Primary" THEN
 
 Print(ProviderName,"Provider Name",rid, 8296)
 Print(P149_EAL2PriRate,"P149_EAL2PriRate",rid, 8296) 
 Print(LAMethod, "EAL Pri 1/EAL Pri 2/EAL Pri 3/NA", rid, 8296)
 
 Result = P149_EAL2PriRate
 
 Else
 
 Result = 0
 
 End if
 
 else
 
 exclude(rid, 8296)
 
 End if
 
 End If
 
 
result = System.Math.Round(result, 6, MidpointRounding.AwayFromZero)
_engine._productResultCache.Add(__key, result)
Return result
 
End Function


Public Function GetProductResult_8295(rid As String) As Decimal
Dim __key = "8295" + rid
If (_engine._productResultCache.ContainsKey(__key)) = True Then Return _engine._productResultCache(__key)

 Dim result = 0
 
 Dim AcademyFilter As Decimal = GetProductResult_7582(rid) 
 Dim FundingBasis As String = _engine.GetDsDataValue(rid, 11187, "2017181")
 
 Print(AcademyFilter,"AcademyFilter",rid, 8295)
 Print(FundingBasis,"FundingBasis",rid, 8295)
 
 If FundingBasis = "Place" Then

 exclude(rid, 8295) 
 
 Else
 
 If AcademyFilter = 17181 Or AcademyFilter = 17182 Or AcademyFilter = 17183 then
 
 Dim EAL2PriCensus As Decimal = _engine.GetDsDataValue(rid, 86934)
 Dim EAL2PriAdj As Decimal = _engine.GetDsDataValue(rid, 86050)
 Dim EAL2PriAdjString as string = _engine.GetDsDataValue(rid, 86050)
 Dim EAL2PriCensusString As String = _engine.GetDsDataValue(rid, 86934)
 Dim LA_AV As Decimal = latoprov(_engine.GetLaDataValue(rid, 86847, Nothing))

 Print(LA_AV,"LA average",rid, 8295)
 Print(EAL2PriCensus, "EAL 2 Pri Census", rid, 8295)
 Print(EAL2PriAdj, "EAL 2 Pri Adjusted", rid, 8295) 
 
 If string.IsNullOrEmpty(EAL2PriCensusString) And string.IsNullOrEmpty(EAL2PriAdjString)Then
 
 Result = LA_AV
 
 Else
 
 If string.IsNullOrEmpty(EAL2PriAdjString) THEN

 Result = EAL2PriCensus
 
 Else
 
 Result = EAL2PriAdj
 
 End if
 
 End if
 
 Else 
 
 exclude(rid, 8295)
 
 End if
 
 End If
 
 
result = System.Math.Round(result, 6, MidpointRounding.AwayFromZero)
_engine._productResultCache.Add(__key, result)
Return result
 
End Function


Public Function GetProductResult_8297(rid As String) As Decimal
Dim __key = "8297" + rid
If (_engine._productResultCache.ContainsKey(__key)) = True Then Return _engine._productResultCache(__key)

 Dim result = 0
 
 Dim AcademyFilter As Decimal = GetProductResult_7582(rid) 
 Dim FundingBasis As String = _engine.GetDsDataValue(rid, 11187, "2017181")
 
 
 Print(AcademyFilter,"AcademyFilter",rid, 8297)
 Print(FundingBasis,"FundingBasis",rid, 8297)
 
 If FundingBasis = "Place" Then

 exclude(rid, 8297) 
 
 Else
 
 
 
 
 
 
 
 
 
 End if
 
 print(FundingBasis, "FundingBasisType",rid, 8297)
 print(AcademyFilter, "AcademyFilter", rid, 8297)
 
 If AcademyFilter = 17181 Or (AcademyFilter = 17182 And FundingBasis = "Estimate") Or (AcademyFilter = 17183 And FundingBasis = "Estimate") THEN
 
 Dim P148_EAL2PriPupils As Decimal = GetProductResult_7667(rid) 
 Dim P149_EAL2PriRate As Decimal = GetProductResult_8296(rid) 
 Dim P147_EAL2PriFactor As Decimal = GetProductResult_8295(rid)  
 Dim P150_EAL2PriSubtotal As Decimal = P148_EAL2PriPupils * P149_EAL2PriRate * P147_EAL2PriFactor
 
 Print(P148_EAL2PriPupils,"P148_EAL2PriPupils",rid, 8297)
 Print(P149_EAL2PriRate,"P149_EAL2PriRate",rid, 8297)
 Print(P147_EAL2PriFactor,"P147_EAL2PriFactor",rid, 8297) 
 
 Result = P150_EAL2PriSubtotal 
 
 else
 
 If (AcademyFilter = 17182 And FundingBasis = "Census") Or (AcademyFilter = 17183 And FundingBasis = "Census") then
 
 Dim EALSelectedbyLA As String = LaToProv(_engine.GetLaDataValue(rid, 86568, Nothing)) 
 
 Print(EALSelectedbyLA,"EALSelectedbyLA",rid, 8297)
 
 If EALSelectedbyLA = "EAL 2 Primary" then
 
 Dim P150_EAL2PriSubtotalAPT As Decimal = _engine.GetDsDataValue(rid, 86130) 
 
 Print(P150_EAL2PriSubtotalAPT,"P150_EAL2PriSubtotalAPT",rid, 8297)
 
 Result = P150_EAL2PriSubtotalAPT
 
 Else
 
 Result = 0
 
 End If
 
 Else 
 
 exclude(rid, 8297)
 
 End If 
 
 End If 
 
 
 
 
result = System.Math.Round(result, 2, MidpointRounding.AwayFromZero)
_engine._productResultCache.Add(__key, result)
Return result
 
End Function


Public Function GetProductResult_8298(rid As String) As Decimal
Dim __key = "8298" + rid
If (_engine._productResultCache.ContainsKey(__key)) = True Then Return _engine._productResultCache(__key)

 Dim result = 0
 
 Dim AcademyFilter As Decimal = GetProductResult_7582(rid) 
 Dim FundingBasis As String = _engine.GetDsDataValue(rid, 11187, "2017181")
 
 Print(AcademyFilter,"AcademyFilter",rid, 8298)
 Print(FundingBasis,"FundingBasis",rid, 8298)
 
 If FundingBasis = "Place" Then
 
 exclude(rid, 8298) 
 
 Else
 
 If AcademyFilter = 17181 Or AcademyFilter = 17182 Or AcademyFilter = 17183 then
 
 Dim P150_EAL2PriSubtotal As Decimal = GetProductResult_8297(rid)  
 Dim Days_Open As Decimal = GetProductResult_8151(rid) 
 Dim Year_Days As Decimal = 365 
 
 Print(P150_EAL2PriSubtotal,"P150 EAL2PriSubtotal",rid, 8298)
 Print(Days_Open,"Days Open",rid, 8298)
 Print(Year_Days,"Year Days",rid, 8298)
 
 Result = (P150_EAL2PriSubtotal) *Divide(Days_Open, Year_Days)
 
 Else
 
 Exclude(rid, 8298) 
 
 End If 
 
 End If
 
 
result = System.Math.Round(result, 6, MidpointRounding.AwayFromZero)
_engine._productResultCache.Add(__key, result)
Return result
 
End Function

Public Function GetProductResult_8300(rid As String) As Decimal
Dim __key = "8300" + rid
If (_engine._productResultCache.ContainsKey(__key)) = True Then Return _engine._productResultCache(__key)

 Dim result = 0
 
 Dim AcademyFilter As Decimal = GetProductResult_7582(rid) 
 Dim FundingBasis As String = _engine.GetDsDataValue(rid, 11187, "2017181")
 
 Print(AcademyFilter,"AcademyFilter",rid, 8300)
 Print(FundingBasis,"FundingBasis",rid, 8300)
 
 If FundingBasis = "Place" Then
 
 exclude(rid, 8300) 
 
 Else
 
 If AcademyFilter = 17181 Or AcademyFilter = 17182 Or AcademyFilter = 17183 then
 
 Dim ProviderName As String = _engine.GetDsDataValue(rid, 9070)
 Dim P154_EAL3PriRate As Decimal = LaToProv(_engine.GetLaDataValue(rid, 86570, Nothing)) 
 Dim LAMethod as string = LaToProv(_engine.GetLaDataValue(rid, 86568, Nothing))
 
 If LAMethod = "EAL 3 Primary" THEN
 
 Print(ProviderName,"Provider Name",rid, 8300)
 Print(P154_EAL3PriRate,"P154_EAL3PriRate",rid, 8300) 
 Print(LAMethod, "EAL Pri 1/EAL Pri 2/EAL Pri 3/NA", rid, 8300)
 
 Result = P154_EAL3PriRate
 
 Else
 
 Result = 0
 
 End if
 
 else
 
 exclude(rid, 8300)
 
 End if
 
 End If
 
 
result = System.Math.Round(result, 6, MidpointRounding.AwayFromZero)
_engine._productResultCache.Add(__key, result)
Return result
 
End Function


Public Function GetProductResult_8299(rid As String) As Decimal
Dim __key = "8299" + rid
If (_engine._productResultCache.ContainsKey(__key)) = True Then Return _engine._productResultCache(__key)

 Dim result = 0
 
 Dim AcademyFilter As Decimal = GetProductResult_7582(rid) 
 Dim FundingBasis As String = _engine.GetDsDataValue(rid, 11187, "2017181")
 
 Print(AcademyFilter,"AcademyFilter",rid, 8299)
 Print(FundingBasis,"FundingBasis",rid, 8299)
 
 If FundingBasis = "Place" Then
 
 exclude(rid, 8299)
 
 Else
 
 If AcademyFilter = 17181 Or AcademyFilter = 17182 Or AcademyFilter = 17183 then
 
 Dim EAL3PriCensus As Decimal = _engine.GetDsDataValue(rid, 86936)
 Dim EAL3PriAdj As Decimal = _engine.GetDsDataValue(rid, 86052)
 Dim EAL3PriAdjString as string = _engine.GetDsDataValue(rid, 86052)
 Dim EAL3PriCensusString As String = _engine.GetDsDataValue(rid, 86936)
 Dim LA_AV As Decimal = latoprov(_engine.GetLaDataValue(rid, 86849, Nothing))

 Print(LA_AV,"LA average",rid, 8299)
 Print(EAL3PriCensus, "EAL 3 Pri Census", rid, 8299)
 Print(EAL3PriAdj, "EAL 3 Pri Adjusted", rid, 8299) 
 
 If string.IsNullOrEmpty(EAL3PriCensusString) And string.IsNullOrEmpty(EAL3PriAdjString) Then
 
 Result = LA_AV
 
 Else

 If string.IsNullOrEmpty(EAL3PriAdjString) THEN
 
 Result = EAL3PriCensus
 
 Else
 
 Result = EAL3PriAdj
 
 End if
 
 End if
 
 Else
 
 exclude(rid, 8299)
 
 End if
 
 End If
 
 
result = System.Math.Round(result, 6, MidpointRounding.AwayFromZero)
_engine._productResultCache.Add(__key, result)
Return result
 
End Function


Public Function GetProductResult_8301(rid As String) As Decimal
Dim __key = "8301" + rid
If (_engine._productResultCache.ContainsKey(__key)) = True Then Return _engine._productResultCache(__key)

 Dim result = 0
 
 Dim AcademyFilter As Decimal = GetProductResult_7582(rid) 
 Dim FundingBasis As String = _engine.GetDsDataValue(rid, 11187, "2017181")
 
 
 Print(AcademyFilter,"AcademyFilter",rid, 8301)
 Print(FundingBasis,"FundingBasis",rid, 8301)
 
 If FundingBasis = "Place" Then
 
 exclude(rid, 8301)
 
 Else
 
 
 
 
 
 
 
 
 
 End if
 
 print(FundingBasis, "FundingBasisType",rid, 8301)
 
 print(AcademyFilter, "AcademyFilter", rid, 8301)
 
 If AcademyFilter = 17181 Or (AcademyFilter = 17182 And FundingBasis = "Estimate") Or (AcademyFilter = 17183 And FundingBasis = "Estimate") THEN
 
 Dim P153_EAL3PriPupils As Decimal = GetProductResult_7667(rid) 
 Dim P154_EAL3PriRate As Decimal = GetProductResult_8300(rid) 
 Dim P152_EAL3PriFactor As Decimal = GetProductResult_8299(rid)  
 Dim P155_EAL3PriSubtotal As Decimal = P153_EAL3PriPupils * P154_EAL3PriRate * P152_EAL3PriFactor
 
 Print(P153_EAL3PriPupils,"P153_EAL3PriPupils",rid, 8301)
 Print(P154_EAL3PriRate,"P154_EAL3PriRate",rid, 8301)
 Print(P152_EAL3PriFactor,"P152_EAL3PriFactor",rid, 8301) 
 
 Result = P155_EAL3PriSubtotal 
 
 Else 

 If (AcademyFilter = 17182 And FundingBasis = "Census") Or (AcademyFilter = 17183 And FundingBasis = "Census") then
 
 Dim EALSelectedbyLA As String = LaToProv(_engine.GetLaDataValue(rid, 86568, Nothing))
 
 
 Print(EALSelectedbyLA,"EALSelectedbyLA",rid, 8301)
 
 If EALSelectedbyLA = "EAL 3 Primary" then
 
 Dim P155_EAL3PriSubtotalAPT As Decimal = _engine.GetDsDataValue(rid, 86130) 
 
 Print(P155_EAL3PriSubtotalAPT,"P155_EAL3PriSubtotalAPT",rid, 8301)
 
 Result = P155_EAL3PriSubtotalAPT
 
 Else
 
 Result = 0
 
 End If
 
 Else 
 
 exclude(rid, 8301)
 
 End If

 End If 
 
 
 
 
result = System.Math.Round(result, 2, MidpointRounding.AwayFromZero)
_engine._productResultCache.Add(__key, result)
Return result
 
End Function


Public Function GetProductResult_8304(rid As String) As Decimal
Dim __key = "8304" + rid
If (_engine._productResultCache.ContainsKey(__key)) = True Then Return _engine._productResultCache(__key)

 Dim result = 0
 
 Dim AcademyFilter As Decimal = GetProductResult_7582(rid) 
 Dim FundingBasis As String = _engine.GetDsDataValue(rid, 11187, "2017181")
 
 Print(AcademyFilter,"AcademyFilter",rid, 8304)
 Print(FundingBasis,"FundingBasis",rid, 8304)
 
 If FundingBasis = "Place" Then

 exclude(rid, 8304)
 
 Else
 
 If AcademyFilter = 17181 Or AcademyFilter = 17182 Or AcademyFilter = 17183 then
 
 Dim P155_EAL3PriSubtotal As Decimal = GetProductResult_8301(rid)  
 Dim Days_Open As Decimal = GetProductResult_8151(rid) 
 Dim Year_Days As Decimal = 365 
 
 Print(P155_EAL3PriSubtotal,"P155 EAL3PriSubtotal",rid, 8304)
 Print(Days_Open,"Days Open",rid, 8304)
 Print(Year_Days,"Year Days",rid, 8304)
 
 Result = (P155_EAL3PriSubtotal) *Divide(Days_Open, Year_Days)
 
 Else
 
 Exclude(rid, 8304) 
 
 End If 
 
 End If
 
 
result = System.Math.Round(result, 6, MidpointRounding.AwayFromZero)
_engine._productResultCache.Add(__key, result)
Return result
 
End Function

Public Function GetProductResult_8306(rid As String) As Decimal
Dim __key = "8306" + rid
If (_engine._productResultCache.ContainsKey(__key)) = True Then Return _engine._productResultCache(__key)

 Dim result = 0
 
 Dim AcademyFilter As Decimal = GetProductResult_7582(rid) 
 Dim FundingBasis As String = _engine.GetDsDataValue(rid, 11187, "2017181")
 
 Print(AcademyFilter,"AcademyFilter",rid, 8306)
 Print(FundingBasis,"FundingBasis",rid, 8306)
 
 If FundingBasis = "Place" Then
 
 exclude(rid, 8306) 
 
 Else
 
 If AcademyFilter = 17181 Or AcademyFilter = 17182 Or AcademyFilter = 17183 then
 
 Dim ProviderName As String = _engine.GetDsDataValue(rid, 9070)
 Dim P160_EAL1SecRate As Decimal = LaToProv(_engine.GetLaDataValue(rid, 86580, Nothing)) 
 Dim LAMethod as string = LaToProv(_engine.GetLaDataValue(rid, 86578, Nothing))
 
 If LAMethod = "EAL 1 Secondary" THEN
 
 Print(ProviderName,"Provider Name",rid, 8306)
 Print(P160_EAL1SecRate,"P160_EAL1SecRate",rid, 8306) 
 Print(LAMethod, "EAL Sec 1/EAL Sec 2/EAL Sec 3/NA", rid, 8306)
 
 Result = P160_EAL1SecRate
 
 Else
 
 Result = 0
 
 End if
 
 else
 
 exclude(rid, 8306)
 
 End If 
 
 End If
 
 
result = System.Math.Round(result, 6, MidpointRounding.AwayFromZero)
_engine._productResultCache.Add(__key, result)
Return result
 
End Function


Public Function GetProductResult_8305(rid As String) As Decimal
Dim __key = "8305" + rid
If (_engine._productResultCache.ContainsKey(__key)) = True Then Return _engine._productResultCache(__key)

 Dim result = 0
 
 Dim AcademyFilter As Decimal = GetProductResult_7582(rid) 
 Dim FundingBasis As String = _engine.GetDsDataValue(rid, 11187, "2017181")
 
 Print(AcademyFilter,"AcademyFilter",rid, 8305)
 Print(FundingBasis,"FundingBasis",rid, 8305)
 
 If FundingBasis = "Place" Then

 exclude(rid, 8305)
 
 Else
 
 If AcademyFilter = 17181 Or AcademyFilter = 17182 Or AcademyFilter = 17183 then
 
 Dim EAL1SecCensus As Decimal = _engine.GetDsDataValue(rid, 86938)
 Dim EAL1SecAdj As Decimal = _engine.GetDsDataValue(rid, 86054)
 Dim EAL1SecAdjString as string = _engine.GetDsDataValue(rid, 86054)
 Dim EAL1SecCensusString As String = _engine.GetDsDataValue(rid, 86938)
 Dim LA_AV As Decimal = latoprov(_engine.GetLaDataValue(rid, 86851, Nothing))

 Print(LA_AV,"LA average",rid, 8305)
 Print(EAL1SecCensus, "EAL 1 Sec Census", rid, 8305)
 Print(EAL1SecAdj, "EAL 1 Sec Adjusted", rid, 8305) 
 
 If string.IsNullOrEmpty(EAL1SecCensusString) And string.IsNullOrEmpty(EAL1SecAdjString) Then
 
 Result = LA_AV
 
 Else
 
 If string.IsNullOrEmpty(EAL1SecAdjString) THEN
 
 Result = EAL1SecCensus
 
 Else
 
 Result = EAL1SecAdj
 
 End if
 
 End if
 
 else
 
 exclude(rid, 8305)
 
 End if

 End If
 
 
result = System.Math.Round(result, 6, MidpointRounding.AwayFromZero)
_engine._productResultCache.Add(__key, result)
Return result
 
End Function


Public Function GetProductResult_8307(rid As String) As Decimal
Dim __key = "8307" + rid
If (_engine._productResultCache.ContainsKey(__key)) = True Then Return _engine._productResultCache(__key)

 Dim result = 0
 
 Dim AcademyFilter As Decimal = GetProductResult_7582(rid) 
 Dim FundingBasis As String = _engine.GetDsDataValue(rid, 11187, "2017181")
 
 
 Print(AcademyFilter,"AcademyFilter",rid, 8307)
 Print(FundingBasis,"FundingBasis",rid, 8307)
 
 If FundingBasis = "Place" Then
 
 exclude(rid, 8307)
 
 Else
 
 
 
 
 
 
 
 
 
 End if
 
 print(FundingBasis, "FundingBasisType",rid, 8307)
 print(AcademyFilter, "AcademyFilter", rid, 8307)
 
 If AcademyFilter = 17181 Or (AcademyFilter = 17182 And FundingBasis = "Estimate") Or (AcademyFilter = 17183 And FundingBasis = "Estimate") THEN
 
 Dim P159_EAL1SecPupils As Decimal = GetProductResult_7673(rid) 
 Dim P160_EAL1SecRate As Decimal = GetProductResult_8306(rid) 
 Dim P158_EAL1SecFactor As Decimal = GetProductResult_8305(rid) 
 Dim P161_EAL1SecSubtotal As Decimal = P159_EAL1SecPupils * P160_EAL1SecRate * P158_EAL1SecFactor
 
 Print(P159_EAL1SecPupils,"P159_EAL1SecPupils",rid, 8307)
 Print(P160_EAL1SecRate,"P160_EAL1SecRate",rid, 8307)
 Print(P158_EAL1SecFactor,"P158_EAL1SecFactor",rid, 8307) 
 
 Result = P161_EAL1SecSubtotal 
 
 Else 

 If (AcademyFilter = 17182 And FundingBasis = "Census") Or (AcademyFilter = 17183 And FundingBasis = "Census") then
 
 Dim EALSelectedbyLA As String = LaToProv(_engine.GetLaDataValue(rid, 86578, Nothing)) 
 
 Print(EALSelectedbyLA,"EALSelectedbyLA",rid, 8307)
 
 If EALSelectedbyLA = "EAL 1 Secondary" then
 
 Dim P161_EAL1SecSubtotalAPT As Decimal = _engine.GetDsDataValue(rid, 86132) 
 
 Print(P161_EAL1SecSubtotalAPT,"P161_EAL1SecSubtotalAPT",rid, 8307)
 
 Result = P161_EAL1SecSubtotalAPT
 
 Else
 
 Result = 0
 
 End If
 
 Else 
 
 exclude(rid, 8307)
 
 End If

 End If 
 
 
 
result = System.Math.Round(result, 2, MidpointRounding.AwayFromZero)
_engine._productResultCache.Add(__key, result)
Return result
 
End Function


 Public Function GetProductResult_8308(rid As String) As Decimal
Dim __key = "8308" + rid
If (_engine._productResultCache.ContainsKey(__key)) = True Then Return _engine._productResultCache(__key)

    Dim result = 0
    
    Dim AcademyFilter As Decimal = GetProductResult_7582(rid) 
    Dim FundingBasis As String = _engine.GetDsDataValue(rid, 11187, "2017181")
     
    Print(AcademyFilter,"AcademyFilter",rid, 8308)
    Print(FundingBasis,"FundingBasis",rid, 8308)
    
    If FundingBasis = "Place" Then
    
    exclude(rid, 8308)
    
    Else
    
        If AcademyFilter = 17181 Or AcademyFilter = 17182 Or AcademyFilter = 17183 then
      
        Dim P161_EAL1SecSubtotal As Decimal = GetProductResult_8307(rid) 
        Dim Days_Open As Decimal = GetProductResult_8151(rid) 
        Dim Year_Days As Decimal = 365                     

        Print(P161_EAL1SecSubtotal,"P161 EAL1SecSubtotal",rid, 8308)
        Print(Days_Open,"Days Open",rid, 8308)
        Print(Year_Days,"Year Days",rid, 8308)
    
        Result = (P161_EAL1SecSubtotal) *Divide(Days_Open, Year_Days)
    
        Else
        
        Exclude(rid, 8308)  
        
        End If  
    
    End If
    
    
result = System.Math.Round(result, 6, MidpointRounding.AwayFromZero)
_engine._productResultCache.Add(__key, result)
Return result
    
End Function
 


 Public Function GetProductResult_8310(rid As String) As Decimal
Dim __key = "8310" + rid
If (_engine._productResultCache.ContainsKey(__key)) = True Then Return _engine._productResultCache(__key)

    Dim result = 0
    
    Dim AcademyFilter As Decimal = GetProductResult_7582(rid) 
    Dim FundingBasis As String = _engine.GetDsDataValue(rid, 11187, "2017181")
     
    Print(AcademyFilter,"AcademyFilter",rid, 8310)
    Print(FundingBasis,"FundingBasis",rid, 8310)
    
    If FundingBasis = "Place" Then
    
    exclude(rid, 8310) 
    
    Else
    
        If AcademyFilter = 17181 Or AcademyFilter = 17182 Or AcademyFilter = 17183 then
        
        Dim ProviderName As String = _engine.GetDsDataValue(rid, 9070)
        Dim P165_EAL2SecRate As Decimal = LaToProv(_engine.GetLaDataValue(rid, 86580, Nothing)) 
        Dim LAMethod as string = LaToProv(_engine.GetLaDataValue(rid, 86578, Nothing))
      
            If LAMethod = "EAL 2 Secondary" THEN
    
            Print(ProviderName,"Provider Name",rid, 8310)
            Print(P165_EAL2SecRate,"P165_EAL2SecRate",rid, 8310)     
            Print(LAMethod, "EAL Sec 1/EAL Sec 2/EAL Sec 3/NA", rid, 8310)
    
            Result = P165_EAL2SecRate
            
            Else
            
            Result = 0
            
            End if
            
            Else exclude(rid, 8310)
        
        End if
    
    End If
    
    
result = System.Math.Round(result, 6, MidpointRounding.AwayFromZero)
_engine._productResultCache.Add(__key, result)
Return result
    
End Function


 Public Function GetProductResult_8309(rid As String) As Decimal
Dim __key = "8309" + rid
If (_engine._productResultCache.ContainsKey(__key)) = True Then Return _engine._productResultCache(__key)

    Dim result = 0
    
    Dim AcademyFilter As Decimal = GetProductResult_7582(rid) 
    Dim FundingBasis As String = _engine.GetDsDataValue(rid, 11187, "2017181")
     
    Print(AcademyFilter,"AcademyFilter",rid, 8309)
    Print(FundingBasis,"FundingBasis",rid, 8309)
    
    If FundingBasis = "Place" Then
    
    exclude(rid, 8309)
    
    Else

        If AcademyFilter = 17181 Or AcademyFilter = 17182 Or AcademyFilter = 17183 then
        
        Dim EAL2SecCensus As Decimal = _engine.GetDsDataValue(rid, 86940)
        Dim EAL2SecAdj As Decimal = _engine.GetDsDataValue(rid, 86056)
        Dim EAL2SecAdjString as string = _engine.GetDsDataValue(rid, 86056)
        Dim EAL2SecCensusString As String = _engine.GetDsDataValue(rid, 86940)
        Dim LA_AV As Decimal = latoprov(_engine.GetLaDataValue(rid, 86853, Nothing))

        Print(LA_AV,"LA average",rid, 8309)
        Print(EAL2SecCensus, "EAL 2 Sec Census", rid, 8309)
        Print(EAL2SecAdj, "EAL 2 Sec Adjusted", rid, 8309)  
     
            If string.IsNullOrEmpty(EAL2SecCensusString) And string.IsNullOrEmpty(EAL2SecAdjString) Then
            
            Result = LA_AV
            
            Else
    
                If string.IsNullOrEmpty(EAL2SecAdjString) THEN
                
                Result = EAL2SecCensus
                
                Else
                
                Result = EAL2SecAdj
                
                End if
            
            End if
            
            Else
            
            Exclude(rid, 8309)
        End if
        
    End If
    
    
result = System.Math.Round(result, 6, MidpointRounding.AwayFromZero)
_engine._productResultCache.Add(__key, result)
Return result
    
End Function


 Public Function GetProductResult_8311(rid As String) As Decimal
Dim __key = "8311" + rid
If (_engine._productResultCache.ContainsKey(__key)) = True Then Return _engine._productResultCache(__key)

    Dim result = 0
    
    Dim AcademyFilter As Decimal = GetProductResult_7582(rid) 
    Dim FundingBasis As String = _engine.GetDsDataValue(rid, 11187, "2017181")
   
          
    Print(AcademyFilter,"AcademyFilter",rid, 8311)
    Print(FundingBasis,"FundingBasis",rid, 8311)
    
    If FundingBasis = "Place" Then
    
    exclude(rid, 8311)
    
    Else
    
       
        
       
                            
           
           
          
            End if
            
            print(FundingBasis, "FundingBasisType",rid, 8311)
            print(AcademyFilter, "AcademyFilter", rid, 8311)
   
                If AcademyFilter = 17181 Or (AcademyFilter = 17182 And FundingBasis = "Estimate") Or (AcademyFilter = 17183 And FundingBasis = "Estimate") THEN
      
                Dim P164_EAL2SecPupils As Decimal = GetProductResult_7673(rid) 
                Dim P165_EAL2SecRate As Decimal = GetProductResult_8310(rid) 
                Dim P163_EAL2SecFactor As Decimal = GetProductResult_8309(rid) 
                Dim P166_EAL2SecSubtotal As Decimal = P164_EAL2SecPupils * P165_EAL2SecRate * P163_EAL2SecFactor
    
                Print(P164_EAL2SecPupils,"P164_EAL2SecPupils",rid, 8311)
                Print(P165_EAL2SecRate,"P165_EAL2SecRate",rid, 8311)
                Print(P163_EAL2SecFactor,"P163_EAL2SecFactor",rid, 8311)                                 

                Result = P166_EAL2SecSubtotal 
     
                Else
    
                    If (AcademyFilter = 17182 And FundingBasis = "Census") Or (AcademyFilter = 17183 And FundingBasis = "Census") then
                    
                    Dim EALSelectedbyLA As String = LaToProv(_engine.GetLaDataValue(rid, 86578, Nothing))                        
                    
                    Print(EALSelectedbyLA,"EALSelectedbyLA",rid, 8311)
                        
                        If EALSelectedbyLA = "EAL 2 Secondary" then
                        
                        Dim P166_EAL2SecSubtotalAPT As Decimal = _engine.GetDsDataValue(rid, 86132) 
                        
                        Print(P166_EAL2SecSubtotalAPT,"P166_EAL2SecSubtotalAPT",rid, 8311)
    
                        Result = P166_EAL2SecSubtotalAPT
                        
                        Else
                        
                        Result = 0
                        
                        End If
                    
                        Else
                    
                        exclude(rid, 8311)
                    
                    End If
  
            End If  
            
   
    
    
result = System.Math.Round(result, 2, MidpointRounding.AwayFromZero)
_engine._productResultCache.Add(__key, result)
Return result
    
End Function



Public Function GetProductResult_8312(rid As String) As Decimal
Dim __key = "8312" + rid
If (_engine._productResultCache.ContainsKey(__key)) = True Then Return _engine._productResultCache(__key)

    Dim result = 0
    
    Dim AcademyFilter As Decimal = GetProductResult_7582(rid) 
    Dim FundingBasis As String = _engine.GetDsDataValue(rid, 11187, "2017181")
     
    Print(AcademyFilter,"AcademyFilter",rid, 8312)
    Print(FundingBasis,"FundingBasis",rid, 8312)
    
    If FundingBasis = "Place" Then
    
    exclude(rid, 8312) 
    
    Else
    
        If AcademyFilter = 17181 Or AcademyFilter = 17182 Or AcademyFilter = 17183 then
      
        Dim P166_EAL2SecSubtotal As Decimal = GetProductResult_8311(rid) 
        Dim Days_Open As Decimal = GetProductResult_8151(rid) 
        Dim Year_Days As Decimal = 365                     

        Print(P166_EAL2SecSubtotal,"P166 EAL2SecSubtotal",rid, 8312)
        Print(Days_Open,"Days Open",rid, 8312)
        Print(Year_Days,"Year Days",rid, 8312)
    
        Result = (P166_EAL2SecSubtotal) *Divide(Days_Open, Year_Days)
    
        Else
        
        Exclude(rid, 8312)  
        
        End If  
    
    End If
    
    
result = System.Math.Round(result, 6, MidpointRounding.AwayFromZero)
_engine._productResultCache.Add(__key, result)
Return result
    
End Function


 Public Function GetProductResult_8314(rid As String) As Decimal
Dim __key = "8314" + rid
If (_engine._productResultCache.ContainsKey(__key)) = True Then Return _engine._productResultCache(__key)

    Dim result = 0
    
    Dim AcademyFilter As Decimal = GetProductResult_7582(rid) 
    Dim FundingBasis As String = _engine.GetDsDataValue(rid, 11187, "2017181")
     
    Print(AcademyFilter,"AcademyFilter",rid, 8314)
    Print(FundingBasis,"FundingBasis",rid, 8314)
    
    If FundingBasis = "Place" Then
    
    exclude(rid, 8314)
    
    Else
    
        If AcademyFilter = 17181 Or AcademyFilter = 17182 Or AcademyFilter = 17183 then
        
        Dim ProviderName As String = _engine.GetDsDataValue(rid, 9070)
        Dim P170_EAL3SecRate As Decimal = LaToProv(_engine.GetLaDataValue(rid, 86580, Nothing)) 
        Dim LAMethod as string = LaToProv(_engine.GetLaDataValue(rid, 86578, Nothing))
      
            If LAMethod = "EAL 3 Secondary" THEN
        
            Print(ProviderName,"Provider Name",rid, 8314)
            Print(P170_EAL3SecRate,"P170_EAL3SecRate",rid, 8314)     
            Print(LAMethod, "EAL Sec 1/EAL Sec 2/EAL Sec 3/NA", rid, 8314)
        
            Result = P170_EAL3SecRate
        
            Else
        
            Result = 0
               
            End If
    
            Else 
    
            Exclude(rid, 8314)    
   
        End if
    
    End If  
    
    
result = System.Math.Round(result, 6, MidpointRounding.AwayFromZero)
_engine._productResultCache.Add(__key, result)
Return result
    
End Function


 Public Function GetProductResult_8313(rid As String) As Decimal
Dim __key = "8313" + rid
If (_engine._productResultCache.ContainsKey(__key)) = True Then Return _engine._productResultCache(__key)

    Dim result = 0
    
    Dim AcademyFilter As Decimal = GetProductResult_7582(rid) 
    Dim FundingBasis As String = _engine.GetDsDataValue(rid, 11187, "2017181")
     
    Print(AcademyFilter,"AcademyFilter",rid, 8313)
    Print(FundingBasis,"FundingBasis",rid, 8313)
    
    If FundingBasis = "Place" Then
    
    exclude(rid, 8313) 
    
    Else
    
        If AcademyFilter = 17181 Or AcademyFilter = 17182 Or AcademyFilter = 17183 then
        
        Dim EAL3SecCensus As Decimal = _engine.GetDsDataValue(rid, 86942)
        Dim EAL3SecAdj As Decimal = _engine.GetDsDataValue(rid, 86058)
        Dim EAL3SecAdjString as string = _engine.GetDsDataValue(rid, 86058)
        Dim EAL3SecCensusString As String = _engine.GetDsDataValue(rid, 86942)
        Dim LA_AV As Decimal = latoprov(_engine.GetLaDataValue(rid, 86855, Nothing))

        Print(LA_AV,"LA average",rid, 8313)
        Print(EAL3SecCensus, "EAL 3 Sec Census", rid, 8313)
        Print(EAL3SecAdj, "EAL 3 Sec Adjusted", rid, 8313)  
    
            If string.IsNullOrEmpty(EAL3SecCensusString) And string.IsNullOrEmpty(EAL3SecAdjString)  Then
            
            Result = LA_AV
            
            Else
    
                If string.IsNullOrEmpty(EAL3SecAdjString) THEN
                
                Result = EAL3SecCensus
                
                Else
                
                Result = EAL3SecAdj
                
                End if
            
            End if
            
            Else
            
            Exclude(rid, 8313)
            
            End if
    End If
    
    
result = System.Math.Round(result, 6, MidpointRounding.AwayFromZero)
_engine._productResultCache.Add(__key, result)
Return result
    
End Function


 Public Function GetProductResult_8315(rid As String) As Decimal
Dim __key = "8315" + rid
If (_engine._productResultCache.ContainsKey(__key)) = True Then Return _engine._productResultCache(__key)

    Dim result = 0
    
    Dim AcademyFilter As Decimal = GetProductResult_7582(rid) 
    Dim FundingBasis As String = _engine.GetDsDataValue(rid, 11187, "2017181")
   
              
    Print(AcademyFilter,"AcademyFilter",rid, 8315)
    Print(FundingBasis,"FundingBasis",rid, 8315)
    
    If FundingBasis = "Place" Then
    
    exclude(rid, 8315) 
   
    Else
    
       
        
       
                
           
            
           
          
            End if
            
            print(FundingBasis, "FundingBasisType",rid, 8315)
            print(AcademyFilter, "AcademyFilter", rid, 8315)
   
                If AcademyFilter = 17181 Or (AcademyFilter = 17182 And FundingBasis = "Estimate") Or (AcademyFilter = 17183 And FundingBasis = "Estimate") THEN
   
                Dim P169_EAL3SecPupils As Decimal = GetProductResult_7673(rid) 
                Dim P170_EAL3SecRate As Decimal = GetProductResult_8314(rid) 
                Dim P168_EAL3SecFactor As Decimal = GetProductResult_8313(rid) 
                Dim P171_EAL3SecSubtotal As Decimal = P169_EAL3SecPupils * P170_EAL3SecRate * P168_EAL3SecFactor
     
                Print(P169_EAL3SecPupils,"P169_EAL3SecPupils",rid, 8315)
                Print(P170_EAL3SecRate,"P170_EAL3SecRate",rid, 8315)
                Print(P168_EAL3SecFactor,"P168_EAL3SecFactor",rid, 8315)                                 

                Result = P171_EAL3SecSubtotal 
     
                Else
    
                    If (AcademyFilter = 17182 And FundingBasis = "Census") Or (AcademyFilter = 17183 And FundingBasis = "Census") then
                    
                    Dim EALSelectedbyLA As String = LaToProv(_engine.GetLaDataValue(rid, 86578, Nothing))                        
                    
                    Print(EALSelectedbyLA,"EALSelectedbyLA",rid, 8315)
                        
                        If EALSelectedbyLA = "EAL 3 Secondary" then
                        
                        Dim P171_EAL3SecSubtotalAPT As Decimal = _engine.GetDsDataValue(rid, 86132) 
                        
                        Print(P171_EAL3SecSubtotalAPT,"P171_EAL3SecSubtotalAPT",rid, 8315)
                               
                        Result = P171_EAL3SecSubtotalAPT
                        
                        Else
                        
                        Result = 0
                        
                        End If
                   
                        Else  
                    
                        exclude(rid, 8315)
                    
                    End If

            End If  
     
   
    
    
result = System.Math.Round(result, 2, MidpointRounding.AwayFromZero)
_engine._productResultCache.Add(__key, result)
Return result
    
End Function


 Public Function GetProductResult_8318(rid As String) As Decimal
Dim __key = "8318" + rid
If (_engine._productResultCache.ContainsKey(__key)) = True Then Return _engine._productResultCache(__key)

    Dim result = 0
    
    Dim AcademyFilter As Decimal = GetProductResult_7582(rid) 
    Dim FundingBasis As String = _engine.GetDsDataValue(rid, 11187, "2017181")
     
    Print(AcademyFilter,"AcademyFilter",rid, 8318)
    Print(FundingBasis,"FundingBasis",rid, 8318)
    
    If FundingBasis = "Place" Then
    
    exclude(rid, 8318)
    
    Else

        If AcademyFilter = 17181 Or AcademyFilter = 17182 Or AcademyFilter = 17183 then
      
        Dim P171_EAL3SecSubtotal As Decimal = GetProductResult_8315(rid) 
        Dim Days_Open As Decimal = GetProductResult_8151(rid) 
        Dim Year_Days As Decimal = 365                     

        Print(P171_EAL3SecSubtotal,"P171 EAL3SecSubtotal",rid, 8318)
        Print(Days_Open,"Days Open",rid, 8318)
        Print(Year_Days,"Year Days",rid, 8318)
    
        Result = (P171_EAL3SecSubtotal) *Divide(Days_Open, Year_Days)
    
        Else
        
        Exclude(rid, 8318)  
        
        End If    
     
    End If
    
    
result = System.Math.Round(result, 6, MidpointRounding.AwayFromZero)
_engine._productResultCache.Add(__key, result)
Return result
    
End Function


Public Function GetProductResult_8319(rid As String) As Decimal
Dim __key = "8319" + rid
If (_engine._productResultCache.ContainsKey(__key)) = True Then Return _engine._productResultCache(__key)

    Dim result = 0
    
    Dim AcademyFilter As Decimal = GetProductResult_7582(rid) 
    Dim FundingBasis As String = _engine.GetDsDataValue(rid, 11187, "2017181")
     
    Print(AcademyFilter,"AcademyFilter",rid, 8319)
    Print(FundingBasis,"FundingBasis",rid, 8319)
    
    If FundingBasis = "Place" Then
    
    exclude(rid, 8319) 
    
    Else

        If AcademyFilter = 17181 Or AcademyFilter = 17182 Or AcademyFilter = 17183 then
     
       
        dim MobPriFactor As Decimal = _engine.GetDsDataValue(rid, 86954)
        dim MobPriFactorAdj As Decimal = _engine.GetDsDataValue(rid, 86070)
        dim MobPriFactorAdjString as string = _engine.GetDsDataValue(rid, 86070)
        Dim MobPriCensusString as string  = _engine.GetDsDataValue(rid, 86954)
        Dim LA_AV As Decimal = latoprov(_engine.GetLaDataValue(rid, 86867, Nothing))

        Print(LA_AV,”LA average”, rid, 8319)
        Print(MobPriFactor, "MobPriFactor Census", rid, 8319)
        Print(MobPriFactorAdj, "MobPriFactorAdj", rid, 8319)  

   

        if string.IsNullOrEmpty(MobPriFactorAdjString) Then
        
            If MobPriFactor > 0.1 then
            
            result = MobPriFactor - 0.1 
            
            else
            
            result = 0
            
            End If 
            
            else
                
                If MobPriFactorAdj > 0.1 then     
                
                result = MobPriFactorAdj - 0.1
                
                else
                
                result = 0 
                
                End if
            
            end if
   
            Else 
            
            exclude(rid, 8319)
        End If
        
    End if
    
    
result = System.Math.Round(result, 6, MidpointRounding.AwayFromZero)
_engine._productResultCache.Add(__key, result)
Return result
    
End Function



Public Function GetProductResult_8321(rid As String) As Decimal
Dim __key = "8321" + rid
If (_engine._productResultCache.ContainsKey(__key)) = True Then Return _engine._productResultCache(__key)

   Dim result = 0
   
   Dim AcademyFilter As Decimal = GetProductResult_7582(rid) 
   Dim FundingBasis As String = _engine.GetDsDataValue(rid, 11187, "2017181")
     
    Print(AcademyFilter,"AcademyFilter",rid, 8321)
    Print(FundingBasis,"FundingBasis",rid, 8321)
    
    If FundingBasis = "Place" Then
    
    exclude(rid, 8321) 
    
    Else
  
        If AcademyFilter = 17181 Or AcademyFilter = 17182 Or AcademyFilter = 17183 then
    
        Dim provider As String = _engine.GetDsDataValue(rid, 9070)
        dim MobPriRate As Decimal = LaToProv(_engine.GetLaDataValue(rid, 86590, Nothing))
        
        result = MobPriRate

        Print(MobPriRate,"SBS_P176_MobPriRate",rid, 8321)  

        else
        
        exclude(rid, 8321)
        
        End If 
    
    End if
    
    
result = System.Math.Round(result, 6, MidpointRounding.AwayFromZero)
_engine._productResultCache.Add(__key, result)
Return result
    
End Function


Public Function GetProductResult_8322(rid As String) As Decimal
Dim __key = "8322" + rid
If (_engine._productResultCache.ContainsKey(__key)) = True Then Return _engine._productResultCache(__key)

    Dim result = 0
   
    Dim AcademyFilter As Decimal = GetProductResult_7582(rid) 
    Dim FundingBasis As String = _engine.GetDsDataValue(rid, 11187, "2017181")
   
    
    If FundingBasis = "Place" Then
            
    exclude(rid, 8322) 
            
    Else
                
   
    
   
                
       
        
       
          
        End if
        
        print(FundingBasis, "FundingBasisType",rid, 8322)
        print(AcademyFilter, "AcademyFilter", rid, 8322)
        
            
            
                If AcademyFilter = 17181 Or (AcademyFilter = 17182 And FundingBasis = "Estimate") Or (AcademyFilter = 17183 And FundingBasis = "Estimate")  then

                dim ProviderType as string = _engine.GetDsDataValue(rid, 9072)
                dim MobPriFactor As Decimal = GetProductResult_8319(rid) 
                dim MobPriPupils As Decimal = GetProductResult_7667(rid) 
                dim MobPriRate As Decimal = GetProductResult_8321(rid) 
    
                Print(MobPriFactor,"Factor",rid, 8322)
                Print(MobPriPupils,"Pupils",rid, 8322)
                Print(MobPriRate,"Rate",rid, 8322)
       
                result = MobPriFactor * MobPriPupils * MobPriRate
     
                else  
   
                    If  (AcademyFilter = 17182 And FundingBasis = "Census") Or (AcademyFilter = 17183 And FundingBasis = "Census") then
                   
                    Dim P177_MobPriSubtotalAPT As Decimal = _engine.GetDsDataValue(rid, 86140) 
                    
                    Print(P177_MobPriSubtotalAPT,"P177_MobPriSubtotalAPT",rid, 8322)
    
                    Result = P177_MobPriSubtotalAPT
                       
                    Else  
                    
                    exclude(rid, 8322)
                    
                    End If  
    
                End if
     
   
     
    
result = System.Math.Round(result, 2, MidpointRounding.AwayFromZero)
_engine._productResultCache.Add(__key, result)
Return result
    
End Function


Public Function GetProductResult_8325(rid As String) As Decimal
Dim __key = "8325" + rid
If (_engine._productResultCache.ContainsKey(__key)) = True Then Return _engine._productResultCache(__key)

    Dim result = 0
    
    Dim AcademyFilter As Decimal = GetProductResult_7582(rid) 
    Dim FundingBasis As String = _engine.GetDsDataValue(rid, 11187, "2017181")
     
    Print(AcademyFilter,"AcademyFilter",rid, 8325)
    Print(FundingBasis,"FundingBasis",rid, 8325)
    
    If FundingBasis = "Place" Then
    
    exclude(rid, 8325)
    
    Else

        If AcademyFilter = 17181 Or AcademyFilter = 17182 Or AcademyFilter = 17183 then

        Dim P177_MobPriSubtotal As Decimal = GetProductResult_8322(rid) 
        Dim Days_Open As Decimal = GetProductResult_8151(rid) 
        Dim Year_Days As Decimal = 365 
                               
        Result = (P177_MobPriSubtotal) *Divide(Days_Open, Year_Days)
    
        Else   
        
        Exclude(rid, 8325)  
        
        End If  
    
    End if
    
    
result = System.Math.Round(result, 6, MidpointRounding.AwayFromZero)
_engine._productResultCache.Add(__key, result)
Return result
    
End Function




Public Function GetProductResult_8326(rid As String) As Decimal
Dim __key = "8326" + rid
If (_engine._productResultCache.ContainsKey(__key)) = True Then Return _engine._productResultCache(__key)

    Dim result As decimal = 0
    
    Dim AcademyFilter As Decimal = GetProductResult_7582(rid) 
    Dim FundingBasis As String = _engine.GetDsDataValue(rid, 11187, "2017181")
     
    Print(AcademyFilter,"AcademyFilter",rid, 8326)
    Print(FundingBasis,"FundingBasis",rid, 8326)
    
    If FundingBasis = "Place" Then
    
    exclude(rid, 8326) 
    
    Else

        If AcademyFilter = 17181 Or AcademyFilter = 17182 Or AcademyFilter = 17183 then
    
        dim ProviderType as string = _engine.GetDsDataValue(rid, 9072)
        dim MobSecFactor As Decimal = _engine.GetDsDataValue(rid, 86956)
        dim MobSecFactorAdj As Decimal = _engine.GetDsDataValue(rid, 86072)
        dim MobSecFactorAdjString as string = _engine.GetDsDataValue(rid, 86072)
        Dim MobSecCensusString as string  = _engine.GetDsDataValue(rid, 86956)
        Dim LA_AV As Decimal = latoprov(_engine.GetLaDataValue(rid, 86869, Nothing))

        Print(LA_AV,”LA average”, rid, 8326)
        Print(MobSecFactor, "MobSecFactor", rid, 8326)
        Print(MobSecFactorAdj, "MobSecFactorAdj", rid, 8326)  

            if string.IsNullOrEmpty(MobSecFactorAdjString) Then
            
                If MobSecFactor > 0.1 then
            
                result = MobSecFactor - 0.1 
            
                else
            
                result = 0
            
                End If 
                
                else
      
                    If MobSecFactorAdj > 0.1 then
            
                    result = MobSecFactorAdj - 0.1
            
                    else
            
                    result = 0 
            
                    End if
            End if
            
        Else 
        
        exclude(rid, 8326)
        
        End if
     
     End if 
     
    
result = System.Math.Round(result, 6, MidpointRounding.AwayFromZero)
_engine._productResultCache.Add(__key, result)
Return result
    
End Function



Public Function GetProductResult_8328(rid As String) As Decimal
Dim __key = "8328" + rid
If (_engine._productResultCache.ContainsKey(__key)) = True Then Return _engine._productResultCache(__key)

    Dim result = 0
    
    Dim AcademyFilter As Decimal = GetProductResult_7582(rid) 
    Dim FundingBasis As String = _engine.GetDsDataValue(rid, 11187, "2017181")
     
    Print(AcademyFilter,"AcademyFilter",rid, 8328)
    Print(FundingBasis,"FundingBasis",rid, 8328)
    
    If FundingBasis = "Place" Then
    
    exclude(rid, 8328) 

    Else

        If AcademyFilter = 17181 Or AcademyFilter = 17182 Or AcademyFilter = 17183 then
        
        Dim provider As String = _engine.GetDsDataValue(rid, 9070)
        dim MobSecRate As Decimal = LaToProv(_engine.GetLaDataValue(rid, 86592, Nothing))
        
        result = MobSecRate

        else
        
        exclude(rid, 8328)
        
        End if
    
    End if
    
    
result = System.Math.Round(result, 6, MidpointRounding.AwayFromZero)
_engine._productResultCache.Add(__key, result)
Return result
    
End Function



 Public Function GetProductResult_8329(rid As String) As Decimal
Dim __key = "8329" + rid
If (_engine._productResultCache.ContainsKey(__key)) = True Then Return _engine._productResultCache(__key)

    Dim result = 0
    
    Dim AcademyFilter As Decimal = GetProductResult_7582(rid) 
    Dim FundingBasis As String = _engine.GetDsDataValue(rid, 11187, "2017181")
   
    
    If FundingBasis = "Place" Then
                
                exclude(rid, 8329) 
                
                Else
    
                
       
        
       
                
           
            
           
          
            End if
                
            print(FundingBasis, "FundingBasisType",rid, 8329)
            print(AcademyFilter, "AcademyFilter", rid, 8329)
         
                
                    
                    If AcademyFilter = 17181 Or (AcademyFilter = 17182 And FundingBasis = "Estimate") Or (AcademyFilter = 17183 And FundingBasis = "Estimate")  then

                    dim ProviderType as string = _engine.GetDsDataValue(rid, 9072)
                    dim MobSecFactor As Decimal = GetProductResult_8326(rid) 
                    dim MobSecPupils As Decimal = GetProductResult_7673(rid) 
                    dim MobSecRate As Decimal = GetProductResult_8328(rid) 
    
                    Print(MobSecFactor,"Factor",rid, 8329)
                    Print(MobSecPupils,"Pupils",rid, 8329)
                    Print(MobSecRate,"Rate",rid, 8329)
   
                    result = MobSecFactor * MobSecPupils * MobSecRate
        
                    else
        
                        If  (AcademyFilter = 17182 And FundingBasis = "Census") Or (AcademyFilter = 17183 And FundingBasis = "Census") then
                        
                        Dim P183_MobSecSubtotalAPT As Decimal = _engine.GetDsDataValue(rid, 86142) 
                        
                        Print(P183_MobSecSubtotalAPT,"P183_MobSecSubtotalAPT",rid, 8329)
    
                        Result = P183_MobSecSubtotalAPT
                       
                        Else  
                        
                        exclude(rid, 8329)
                        
                        End If  
        
                    End if
       
        
        
result = System.Math.Round(result, 2, MidpointRounding.AwayFromZero)
_engine._productResultCache.Add(__key, result)
Return result
        
End Function



Public Function GetProductResult_8332(rid As String) As Decimal
Dim __key = "8332" + rid
If (_engine._productResultCache.ContainsKey(__key)) = True Then Return _engine._productResultCache(__key)

    Dim result = 0
    
    Dim AcademyFilter As Decimal = GetProductResult_7582(rid) 
    Dim FundingBasis As String = _engine.GetDsDataValue(rid, 11187, "2017181")
     
    Print(AcademyFilter,"AcademyFilter",rid, 8332)
    Print(FundingBasis,"FundingBasis",rid, 8332)
    
    If FundingBasis = "Place" Then
    
    exclude(rid, 8332) 

    Else

        If AcademyFilter = 17181 Or AcademyFilter = 17182 Or AcademyFilter = 17183 then

        Dim P183_MobSecSubtotal As Decimal = GetProductResult_8329(rid) 
        Dim Days_Open As Decimal = GetProductResult_8151(rid) 
        Dim Year_Days As Decimal = 365 
                               
        Result = (P183_MobSecSubtotal) *Divide(Days_Open, Year_Days)
    
        Else   
        
        Exclude(rid, 8332)  
        
        End If  
    
    End if
    
    
result = System.Math.Round(result, 6, MidpointRounding.AwayFromZero)
_engine._productResultCache.Add(__key, result)
Return result
    
End Function


Public Function GetProductResult_8333(rid As String) As Decimal
Dim __key = "8333" + rid
If (_engine._productResultCache.ContainsKey(__key)) = True Then Return _engine._productResultCache(__key)

    Dim result = 0
    
    Dim AcademyFilter As Decimal = GetProductResult_7582(rid) 
    Dim FundingBasis As String = _engine.GetDsDataValue(rid, 11187, "2017181")
     
    Print(AcademyFilter,"AcademyFilter",rid, 8333)
    Print(FundingBasis,"FundingBasis",rid, 8333)
    
    If FundingBasis = "Place" Then

    exclude(rid, 8333) 
    
    Else
    
    End if
        
        If AcademyFilter = 17181 Or AcademyFilter = 17182 Or AcademyFilter = 17183 then
  
        Dim Phase As String = _engine.GetDsDataValue(rid, 86884)
        Dim IandA_Phase As String = _engine.GetDsDataValue(rid, 85982)
        
        Print(Phase,"Phase",rid, 8333)
        Print(IandA_Phase,"IandA_Phase",rid, 8333)
    
            If  string.IsNullOrEmpty(IandA_Phase) then

                If Phase Like "Primary" then
                
                result = 1
                
                    ElseIf Phase Like "Middle-deemed Primary" then
                    
                    result = 2
                    
                        ElseIf Phase Like "Secondary" then
                    
                        result = 3
                            
                            ElseIf Phase Like "Middle-deemed Secondary" then
                            
                            result = 4
                                
                                ElseIf Phase Like "All-through" then
                                
                                result = 5
                                    
                                Else
                                
                                result =0
                                
                                End If
        
                                Else 
                                    
                                    If IandA_Phase Like "Primary" then
                                    
                                    result = 1
                                    
                                        ElseIf IandA_Phase Like "Middle-deemed Primary" then
                                    
                                        result = 2
                                            
                                            ElseIf IandA_Phase Like "Secondary" then
                                            
                                            result = 3
                                                
                                                ElseIf IandA_Phase Like "Middle-deemed Secondary" then
                                                
                                                result = 4
                                                
                                                    ElseIf IandA_Phase Like "All-through" then
                                                
                                                    result = 5
                                                
                                                    Else
                                                
                                                    result =0
                                    End If
                End if  
                
                Else
                
                exclude(rid, 8333)
            
    End If
    
    
_engine._productResultCache.Add(__key, result)
Return result
     
End Function


Public Function GetProductResult_8334(rid As String) As Decimal
Dim __key = "8334" + rid
If (_engine._productResultCache.ContainsKey(__key)) = True Then Return _engine._productResultCache(__key)

    Dim result = 0
    
    Dim AcademyFilter As Decimal = GetProductResult_7582(rid) 
    Dim FundingBasis As String = _engine.GetDsDataValue(rid, 11187, "2017181")
     
    Print(AcademyFilter,"AcademyFilter",rid, 8334)
    Print(FundingBasis,"FundingBasis",rid, 8334)
    
    If FundingBasis = "Place" Then
    
    exclude(rid, 8334)
    
    Else

        If AcademyFilter = 17181 Or AcademyFilter = 17182 Or AcademyFilter = 17183 then
        
       
        Dim PriFlag As String = LAtoProv(_engine.GetLaDataValue(rid, 86662, Nothing))  
  
            If PriFlag = "Tapered" then
            
            result = 1
        
            Else 
             
            result =   0
            
            End If 

            else
            
            exclude(rid, 8334)
            
        End if
    
    End if
    
    
_engine._productResultCache.Add(__key, result)
Return result
    
End Function


Public Function GetProductResult_8335(rid As String) As Decimal
Dim __key = "8335" + rid
If (_engine._productResultCache.ContainsKey(__key)) = True Then Return _engine._productResultCache(__key)

    Dim result = 0
    
    Dim AcademyFilter As Decimal = GetProductResult_7582(rid) 
    Dim FundingBasis As String = _engine.GetDsDataValue(rid, 11187, "2017181")
     
    Print(AcademyFilter,"AcademyFilter",rid, 8335)
    Print(FundingBasis,"FundingBasis",rid, 8335)
    
    If FundingBasis = "Place" Then
    
    exclude(rid, 8335) 
    
    Else

        If AcademyFilter = 17181 Or AcademyFilter = 17182 Or AcademyFilter = 17183 then
        
       
        Dim MidFlag As String = LAtoProv(_engine.GetLaDataValue(rid, 86666, Nothing))  
  
            If MidFlag = "Tapered" then
            
            result = 1
        
            Else 
              
            result =   0
            
            End If 
            
            else
            
            exclude(rid, 8335)
        End if
    
    End if
     
    
result = System.Math.Round(result, 6, MidpointRounding.AwayFromZero)
_engine._productResultCache.Add(__key, result)
Return result
    
End Function


Public Function GetProductResult_8336(rid As String) As Decimal
Dim __key = "8336" + rid
If (_engine._productResultCache.ContainsKey(__key)) = True Then Return _engine._productResultCache(__key)

    Dim result = 0
    
    Dim AcademyFilter As Decimal = GetProductResult_7582(rid) 
    Dim FundingBasis As String = _engine.GetDsDataValue(rid, 11187, "2017181")
     
    Print(AcademyFilter,"AcademyFilter",rid, 8336)
    Print(FundingBasis,"FundingBasis",rid, 8336)
    
    If FundingBasis = "Place" Then
    
    exclude(rid, 8336)
    
    Else

        If AcademyFilter = 17181 Or AcademyFilter = 17182 Or AcademyFilter = 17183 then
    
       
        Dim SecFlag As String = LAtoProv(_engine.GetLaDataValue(rid, 86664, Nothing))  
  
            If SecFlag = "Tapered" then
            
            result = 1
        
            Else 
             
            result =   0
            
            End If 

            else
            
            exclude(rid, 8336)
        
        End if
    
    End if
    
    
result = System.Math.Round(result, 6, MidpointRounding.AwayFromZero)
_engine._productResultCache.Add(__key, result)
Return result
    
End Function


 Public Function GetProductResult_8337(rid As String) As Decimal
Dim __key = "8337" + rid
If (_engine._productResultCache.ContainsKey(__key)) = True Then Return _engine._productResultCache(__key)

    Dim result = 0
    
    Dim AcademyFilter As Decimal = GetProductResult_7582(rid) 
    Dim FundingBasis As String = _engine.GetDsDataValue(rid, 11187, "2017181")
     
    Print(AcademyFilter,"AcademyFilter",rid, 8337)
    Print(FundingBasis,"FundingBasis",rid, 8337)
    
    If FundingBasis = "Place" Then
    
    exclude(rid, 8337)
    
    Else

        If AcademyFilter = 17181 Or AcademyFilter = 17182 Or AcademyFilter = 17183 then
        
       
        Dim AllThruFlag As String = LAtoProv(_engine.GetLaDataValue(rid, 86668, Nothing))  
  
            If AllThruFlag = "Tapered" then
            
            result = 1
        
            Else 
             
            result =   0
            
            End If 

            else
            
            exclude(rid, 8337)
            
            End if
    
    End if
    
    
result = System.Math.Round(result, 6, MidpointRounding.AwayFromZero)
_engine._productResultCache.Add(__key, result)
Return result

End Function


Public Function GetProductResult_8341(rid As String) As Decimal
Dim __key = "8341" + rid
If (_engine._productResultCache.ContainsKey(__key)) = True Then Return _engine._productResultCache(__key)

    Dim result = 0
    
     Dim AcademyFilter As Decimal = GetProductResult_7582(rid) 
     Dim FundingBasis As String = _engine.GetDsDataValue(rid, 11187, "2017181")
     Dim Phase As Decimal = GetProductResult_8333(rid) 
     
     Print(AcademyFilter,"AcademyFilter",rid, 8341)
     Print(FundingBasis,"FundingBasis",rid, 8341)
     Print(Phase,"Phase",rid, 8341)
    
     If FundingBasis = "Place" Then
        
     exclude(rid, 8341) 
              
             
              
     Else

        If (AcademyFilter = 17181 Or AcademyFilter = 17182  Or AcademyFilter = 17183) And Phase = 1  then
        
       
        Dim SparsityDistThreshold As Decimal = latoprov(_engine.GetLaDataValue(rid, 86670, Nothing))   
        Dim SparsityDistThresholdString As String = latoprov(_engine.GetLaDataValue(rid, 86670, Nothing))   
        
            If string.IsNullOrEmpty(SparsityDistThresholdString) THEN 
            
            Result = 2
            
            Else
            
            Result =  SparsityDistThreshold
            
            End If
        
        else
        
        If (AcademyFilter = 17181 Or AcademyFilter = 17182  Or AcademyFilter = 17183) And (Phase = 2 Or Phase = 4) then
        
        Dim SparsityDistThreshold As Decimal = latoprov(_engine.GetLaDataValue(rid, 86674, Nothing))
        Dim SparsityDistThresholdString As String = latoprov(_engine.GetLaDataValue(rid, 86674, Nothing))
        
        If String.IsNullOrEmpty(SparsityDistThresholdString) then
        
        Result = 2
        
        Else
        Result = SparsityDistThreshold
        
        End If
                                                                                                               
        
            else
        
                If (AcademyFilter = 17181 Or AcademyFilter = 17182  Or AcademyFilter = 17183) And (Phase = 3)  then
                
               
                Dim SparsityDistThreshold As Decimal = latoprov(_engine.GetLaDataValue(rid, 86672, Nothing))   
                Dim SparsityDistThresholdString As String = latoprov(_engine.GetLaDataValue(rid, 86672, Nothing))   
            
                    If string.IsNullOrEmpty(SparsityDistThresholdString) THEN 
                    
                    Result = 3
                    
                    Else
                    
                    Result =  SparsityDistThreshold
                    
                    End If
                    
                   
                     else
                     
                     If (AcademyFilter = 17181 Or AcademyFilter = 17182  Or AcademyFilter = 17183) And (Phase = 5)  then
                    
                     Dim SparsityDistThreshold As Decimal = latoprov(_engine.GetLaDataValue(rid, 86676, Nothing))
                     Dim SparsityDistThresholdString As String = latoprov(_engine.GetLaDataValue(rid, 86676, Nothing))
                     
                     If String.IsNullOrEmpty(SparsityDistThresholdString) THEN
                     
                     Result = 2
                     
                     Else
                     
                     Result = SparsityDistThreshold
                     
                     End If
                    
                   
                    else
                        
                        If (AcademyFilter = 17181 Or AcademyFilter = 17182  Or AcademyFilter = 17183) And (Phase = 0)  then
                        
                       

                        Result = 0

                        Else
                        
                        exclude(rid, 8341)
                        
                        End if
               
               End If         
        
        End If
        
        End If
        
        End If
    
    End if
    
    
result = System.Math.Round(result, 6, MidpointRounding.AwayFromZero)
_engine._productResultCache.Add(__key, result)
Return result
    
End Function


Public Function GetProductResult_8340(rid As String) As Decimal
Dim __key = "8340" + rid
If (_engine._productResultCache.ContainsKey(__key)) = True Then Return _engine._productResultCache(__key)

    Dim result = 0
    
    Dim AcademyFilter As Decimal = GetProductResult_7582(rid) 
    Dim FundingBasis As String = _engine.GetDsDataValue(rid, 11187, "2017181")
    Dim Phase As Decimal = GetProductResult_8333(rid) 
        
    Dim AvDistPriIandA As Decimal = _engine.GetDsDataValue(rid, 86074)
    Dim AvDistPriIandAstr As string = _engine.GetDsDataValue(rid, 86074)
    Dim AvDistPri As Decimal = _engine.GetDsDataValue(rid, 86958)
    Dim AvDistPriString as string  = _engine.GetDsDataValue(rid, 86958)
    Dim AvDistSecIandA As Decimal = _engine.GetDsDataValue(rid, 86076)
    Dim AvDistSecIandAstr As String = _engine.GetDsDataValue(rid, 86076)
    Dim AvDistSec As Decimal = _engine.GetDsDataValue(rid, 86960)
    Dim AvDistSecString As String = _engine.GetDsDataValue(rid, 86960)
                              
    Dim PriResult As Decimal
    Dim SecResult As Decimal


    Print(AcademyFilter,"AcademyFilter",rid, 8340)
    Print(FundingBasis,"FundingBasis",rid, 8340)
    Print(AvDistPri, "AvDistPri", rid, 8340)
    Print(AvDistSec, "AvDistSec", rid, 8340)
    Print(Phase,"Phase",rid, 8340) 
    Print(AvDistPriIandA, "Av Dist Pri inputs And Adj",rid, 8340)
    Print(AvDistPri, "Av Dist Pri Pupil Char",rid, 8340)   
    Print(AvDistSecIandA, "Av Dist Sec inputs And Adj",rid, 8340)
    Print(AvDistSec, "Av Dist Sec Pupil Char",rid, 8340)  
      
    If FundingBasis = "Place" Then
    
    exclude(rid, 8340) 
    
    ElseIf (AcademyFilter = 17181 Or AcademyFilter = 17182  Or AcademyFilter = 17183) then

        If string.IsNullOrEmpty(AvDistPriString) then 
        
            If string.IsNullOrEmpty(AvDistPriIandAstr) then
            
            PriResult = 0
                    
            else
                    
            PriResult =   AvDistPriIandA  
                    
            End if           
                    
                ElseIf string.IsNullOrEmpty(AvDistPriIandAstr) then

                PriResult =  AvDistPri
                 
                Else
                 
                PriResult =   AvDistPriIandA
          
                End If 
           
                    If string.IsNullOrEmpty(AvDistSecString) then        
                    
                    SecResult = 0
                 
                        ElseIf string.IsNullOrEmpty(AvDistSecIandAstr) then
                                  
                        SecResult =  AvDistSec
                 
                        Else
                 
                        SecResult =   AvDistSecIandA
         
                        End If        
                                 
                            If Phase = 1 then
                            
                            Result = PriResult
                         
                                ElseIf Phase = 2 Or Phase = 3 Or Phase = 4 Or Phase = 5 then
                            
                                Result = SecResult
                         
                                   
                                    
                                   
                                   
                                    
                                    Else
                                    
                                    Result = 0
                                    
                                    End If
                                    
                                    Else
                                    
                                    Exclude(rid, 8340)    
                    End If
    
                    Print(PriResult,"PriResult",rid, 8340)
                    Print(SecResult,"SecResult",rid, 8340) 
    
    
result = System.Math.Round(result, 6, MidpointRounding.AwayFromZero)
_engine._productResultCache.Add(__key, result)
Return result

End Function              


Public Function GetProductResult_8342(rid As String) As Decimal
Dim __key = "8342" + rid
If (_engine._productResultCache.ContainsKey(__key)) = True Then Return _engine._productResultCache(__key)

    Dim result = 0
    
    Dim AcademyFilter As Decimal = GetProductResult_7582(rid) 
    Dim FundingBasis As String = _engine.GetDsDataValue(rid, 11187, "2017181")
     
    Print(AcademyFilter,"AcademyFilter",rid, 8342)
    Print(FundingBasis,"FundingBasis",rid, 8342)
    
    If FundingBasis = "Place" Then
    
    exclude(rid, 8342) 
    
    Else

        If AcademyFilter = 17181 Or AcademyFilter = 17182 Or AcademyFilter = 17183 then
    
        Dim P192_SparsityDistThreshold As Decimal = GetProductResult_8341(rid) 
        Dim P191_SparsityDistance   As Decimal = GetProductResult_8340(rid) 
     
            If  P191_SparsityDistance >= P192_SparsityDistThreshold Then 
            
            Print( P192_SparsityDistThreshold,"P192_SparsityDistThreshold",rid, 8342)
            Print( P191_SparsityDistance, "P191_SparsityDistance",rid, 8342)
         
            RESULT =    1
      
            Else 
      
            Result =    0
            
            End If

            else

            exclude(rid, 8342)
            
            End if
    
    End if
    
    
result = System.Math.Round(result, 6, MidpointRounding.AwayFromZero)
_engine._productResultCache.Add(__key, result)
Return result
    
End Function


Public Function GetProductResult_8344(rid As String) As Decimal
Dim __key = "8344" + rid
If (_engine._productResultCache.ContainsKey(__key)) = True Then Return _engine._productResultCache(__key)

    Dim result = 0
    
    Dim AcademyFilter As Decimal = GetProductResult_7582(rid) 
    Dim FundingBasis As String = _engine.GetDsDataValue(rid, 11187, "2017181")
    Dim Phase As String = GetProductResult_8333(rid) 
    
    Print(AcademyFilter,"AcademyFilter",rid, 8344)
    Print(FundingBasis,"FundingBasis",rid, 8344)
    Print(Phase,"Phase",rid, 8344)
    
    If FundingBasis = "Place" Then
    
    exclude(rid, 8344)
    
    Else

        If (AcademyFilter = 17181 Or AcademyFilter = 17182 Or AcademyFilter = 17183) And (Phase = 1) then
        
       
        Dim P195_SparsityYGThresholdPRI As Decimal = LatoProv(_engine.GetLaDataValue(rid, 86678, Nothing))
        Dim P195_SparsityYGThresholdPRIString As String = LatoProv(_engine.GetLaDataValue(rid, 86678, Nothing))
       
            If string.IsNullOrEmpty(P195_SparsityYGThresholdPRIString) THEN 
            
            Result = 21.4
            
            Else
            
            Result =  P195_SparsityYGThresholdPRI
            
            End If
   
            Else
            
                If (AcademyFilter = 17181 Or AcademyFilter = 17182  Or AcademyFilter = 17183) And (Phase = 2 Or Phase = 4) then
                
               
                Dim P195_SparsityYGThresholdMID As Decimal = LatoProv(_engine.GetLaDataValue(rid, 86682, Nothing))
                Dim P195_SparsityYGThresholdMIDString As String = LatoProv(_engine.GetLaDataValue(rid, 86682, Nothing))
   
                    If string.IsNullOrEmpty(P195_SparsityYGThresholdMIDString) THEN 
                    
                    Result = 69.2
                    
                    Else
                    
                    Result =  P195_SparsityYGThresholdMID
                    
                    End If
   
                    Else
                        
                        If (AcademyFilter = 17181 Or AcademyFilter = 17182 Or AcademyFilter = 17183) And (Phase = 3) then
                        
                       
                        Dim P195_SparsityYGThresholdSEC As Decimal = LatoProv(_engine.GetLaDataValue(rid, 86680, Nothing))
                        Dim P195_SparsityYGThresholdSECString As String = LatoProv(_engine.GetLaDataValue(rid, 86680, Nothing))
   
                                If string.IsNullOrEmpty(P195_SparsityYGThresholdSECString) THEN 
                                
                                Result =  120
                                
                                Else
                                
                                Result =  P195_SparsityYGThresholdSEC
                                
                                End If
   
                                Else
  
                                    If (AcademyFilter = 17181 Or AcademyFilter = 17182 Or AcademyFilter = 17183) And (Phase = 5) then
                                    
                                   
                                    Dim P195_SparsityYGThresholdALL As Decimal = LatoProv(_engine.GetLaDataValue(rid, 86684, Nothing))
                                    Dim P195_SparsityYGThresholdALLString As String = LatoProv(_engine.GetLaDataValue(rid, 86684, Nothing))
   
                                        If string.IsNullOrEmpty(P195_SparsityYGThresholdALLString) THEN 
                                        
                                        Result =  62.5
                                        
                                        Else
                                        
                                        Result =  P195_SparsityYGThresholdALL
                                        
                                        End If
   
                                        Else  

                                                If (AcademyFilter = 17181 Or AcademyFilter = 17182 Or AcademyFilter = 17183) And (Phase = 0) then
                                                
                                               
                                                
                                                Result =    0
                 
                                                Else  
                                                     
                                                exclude(rid, 8344)
                                                
                                                End if
                                        End If
                                    
                                    End If                
                End If            
       
        End If
    
    End if
    
    
result = System.Math.Round(result, 6, MidpointRounding.AwayFromZero)
_engine._productResultCache.Add(__key, result)
Return result
    
End Function


Public Function GetProductResult_12260(rid As String) As Decimal
 Dim result As Decimal = 0
 
Dim __key = "12260" + rid
If (_engine._productResultCache.ContainsKey(__key)) = True Then Return _engine._productResultCache(__key)

 Dim EstNOR_Rep As Decimal = _engine.GetDsDataValue(rid, 87272)
 Dim EstNOR_Y1 As Decimal = _engine.GetDsDataValue(rid, 87274)
 Dim EstNOR_Y2 As Decimal = _engine.GetDsDataValue(rid, 87276)
 Dim EstNOR_Y3 As Decimal = _engine.GetDsDataValue(rid, 87278)
 Dim EstNOR_Y4 As Decimal = _engine.GetDsDataValue(rid, 87280)
 Dim EstNOR_Y5 As Decimal = _engine.GetDsDataValue(rid, 87282)
 Dim EstNOR_Y6 As Decimal = _engine.GetDsDataValue(rid, 87284)
 Dim Rep_YG As Decimal
 Dim Y1_YG As Decimal
 Dim Y2_YG As Decimal
 Dim Y3_YG As Decimal
 Dim Y4_YG As Decimal
 Dim Y5_YG As Decimal
 Dim Y6_YG As Decimal
 
 Print(EstNOR_Rep,"Reception",rid, 12260)
 Print(EstNOR_Y1,"Year 1",rid, 12260)
 Print(EstNOR_Y2,"Year 2",rid, 12260)
 Print(EstNOR_Y3,"Year 3",rid, 12260)
 Print(EstNOR_Y4,"Year 4",rid, 12260)
 Print(EstNOR_Y5,"Year 5",rid, 12260)
 Print(EstNOR_Y6,"Year 6",rid, 12260)
 
 If (EstNOR_Rep > 0) then
 Rep_YG = 1
 Else
 Rep_YG = 0
 End if
 
 If (EstNOR_Y1 > 0) then
 Y1_YG = 1
 Else
 Y1_YG = 0
 End if
 
 If (EstNOR_Y2 > 0) Then
 Y2_YG = 1
 Else
 Y2_YG = 0
 End if
 
 If (EstNOR_Y3 > 0) Then
 Y3_YG = 1
 Else
 Y3_YG = 0
 End if
 
 If (EstNOR_Y4 > 0) Then
 Y4_YG = 1
 Else
 Y4_YG = 0
 End if
 
 If (EstNOR_Y5 > 0) Then
 Y5_YG = 1
 Else
 Y5_YG = 0
 End if
 
 If (EstNOR_Y6 > 0) Then
 Y6_YG = 1
 Else
 Y6_YG = 0
 End if
 
 Result = (Rep_YG + Y1_YG + Y2_YG + Y3_YG + Y4_YG + Y5_YG + Y6_YG)
 
 
_engine._productResultCache.Add(__key, result)
Return result
End Function


 Public Function GetProductResult_7697(rid As String) As Decimal
Dim __key = "7697" + rid
If (_engine._productResultCache.ContainsKey(__key)) = True Then Return _engine._productResultCache(__key)

 Dim result = 0
 
 Dim AcadFilter As Decimal = GetProductResult_7582(rid) 
 Dim FundingBasis = GetProductResult_7594(rid) 
 Dim Primary_YG_Census = _engine.GetDsDataValue(rid, 86892)
 Dim Primary_Middle_YG_Census = _engine.GetDsDataValue(rid, 86888)
 Dim IsNull As Boolean 
 IsNull = IIF(_engine.GetApprovedDsDataValue(rid, 85992),false,true)
 Dim Primary_YG_Adj As Decimal = _engine.GetDsDataValue(rid, 85992)
 Dim Primary_YG_Estimate = GetProductResult_12260(rid) 
 Dim Primary_Year_Groups As Decimal
 
 
 Print(IsNull, "I&A NULL check", rid, 7697)
 Print(Primary_YG_Adj, "PYG from I&A", rid, 7697)
 Print(FundingBasis, "Funding Basis", rid, 7697)
 Print(Primary_YG_Census, "Primary YG Census", rid, 7697)
 Print(Primary_Middle_YG_Census, "Middle YG Census", rid, 7697)
 Print(Primary_YG_Estimate, "Pri Estimate YG Census", rid, 7697)
 
 
 
 
If currentscenario.periodid = 2017181 And (AcadFilter = 17181 Or AcadFilter = 17182 Or AcadFilter = 17183) Then
 If FundingBasis = 1 And IsNull = False Then
 Primary_Year_Groups = Primary_YG_Adj
 ElseIf FundingBasis = 1 And IsNull = True Then
 Primary_Year_Groups = Primary_YG_Census
 Else If FundingBasis = 2 Then
 Primary_Year_Groups = Primary_YG_Estimate
 Else Primary_Year_Groups = 0
 End If 
 Else Exclude(rid, 7697) 
End If
 
 
 
 result = Primary_Year_Groups
 
 
 
_engine._productResultCache.Add(__key, result)
Return result
End Function


Public Function GetProductResult_8363(rid As String) As Decimal
Dim __key = "8363" + rid
If (_engine._productResultCache.ContainsKey(__key)) = True Then Return _engine._productResultCache(__key)

    Dim result = 0
    
    Dim AcademyFilter As Decimal = GetProductResult_7582(rid) 
    Dim FundingBasis As String = _engine.GetDsDataValue(rid, 11187, "2017181")
     
    Print(AcademyFilter,"AcademyFilter",rid, 8363)
    Print(FundingBasis,"FundingBasis",rid, 8363)
    
    If FundingBasis = "Place" Then
    
    exclude(rid, 8363) 
    
    Else

        If AcademyFilter = 17181 Or AcademyFilter = 17182 Or AcademyFilter = 17183 then
        
        Dim PYG As Decimal = GetProductResult_7697(rid) 
    
        result =   PYG
        
        else
        
        exclude(rid, 8363)
        
        End if
    
    End if
    
    
_engine._productResultCache.Add(__key, result)
Return result
    
End Function


Public Function GetProductResult_12263(rid As String) As Decimal
 Dim result As Decimal = 0
 
Dim __key = "12263" + rid
If (_engine._productResultCache.ContainsKey(__key)) = True Then Return _engine._productResultCache(__key)

 Dim EstNOR_Y7 As Decimal = _engine.GetDsDataValue(rid, 87286)
 Dim EstNOR_Y8 As Decimal = _engine.GetDsDataValue(rid, 87288)
 Dim EstNOR_Y9 As Decimal = _engine.GetDsDataValue(rid, 87290)
 Dim EstNOR_Y10 As Decimal = _engine.GetDsDataValue(rid, 87292)
 Dim EstNOR_Y11 As Decimal = _engine.GetDsDataValue(rid, 87294)
 Dim Y7_YG As Decimal
 Dim Y8_YG As Decimal
 Dim Y9_YG As Decimal
 Dim Y10_YG As Decimal
 Dim Y11_YG As Decimal
 
 Print(EstNOR_Y7,"Year 7",rid, 12263)
 Print(EstNOR_Y8,"Year 8",rid, 12263)
 Print(EstNOR_Y9,"Year 9",rid, 12263)
 Print(EstNOR_Y10,"Year 10",rid, 12263)
 Print(EstNOR_Y11,"Year 11",rid, 12263)
 
 If (EstNOR_Y7 > 0) Then
 Y7_YG = 1
 Else
 Y7_YG = 0
 End if
 
 If (EstNOR_Y8 > 0) Then
 Y8_YG = 1 
 Else
 Y8_YG = 0
 End if
 
 If (EstNOR_Y9 > 0) Then
 Y9_YG = 1
 Else
 Y9_YG = 0
 End if
 
 If (EstNOR_Y10 > 0) Then
 Y10_YG = 1
 Else
 Y10_YG = 0
 End if
 
 If (EstNOR_Y11 > 0) Then
 Y11_YG = 1
 Else
 Y11_YG = 0
 End if
 
 Result = (Y7_YG + Y8_YG + Y9_YG + Y10_YG + Y11_YG)
 
 
_engine._productResultCache.Add(__key, result)
Return result
End Function


 Public Function GetProductResult_7698(rid As String) As Decimal
Dim __key = "7698" + rid
If (_engine._productResultCache.ContainsKey(__key)) = True Then Return _engine._productResultCache(__key)

 Dim result = 0
 
 
 
 
 
 Dim AcadFilter As Decimal = GetProductResult_7582(rid) 
 Dim FundingBasis = GetProductResult_7594(rid) 
 Dim Sec_YG_Census = _engine.GetDsDataValue(rid, 86894)
 Dim Sec_Middle_YG_Census = _engine.GetDsDataValue(rid, 86890)
 Dim IsNull As Boolean 
 IsNull = IIF(_engine.GetApprovedDsDataValue(rid, 85994),false,true)
 Dim Sec_YG_Adj As Decimal = _engine.GetDsDataValue(rid, 85994)
 Dim Sec_YG_Estimate = GetProductResult_12263(rid) 
 Dim Secondary_Year_Groups As Decimal
 
 Print(IsNull, "I&A NULL check", rid, 7698)
 Print(Sec_YG_Adj, "SYG from I&A", rid, 7698)
 Print(FundingBasis, "Funding Basis", rid, 7698)
 Print(Sec_YG_Census, "Sec YG Census", rid, 7698)
 Print(Sec_Middle_YG_Census, "Sec Middle YG Census", rid, 7698)
 Print(Sec_YG_Estimate, "Sec Estimate YG", rid, 7698)
 
 
 
 
If currentscenario.periodid = 2017181 And (AcadFilter = 17181 Or AcadFilter = 17182 Or AcadFilter = 17183) Then
 If FundingBasis = 1 And IsNull = False Then
 Secondary_Year_Groups = Sec_YG_Adj
 ElseIf FundingBasis = 1 And IsNull = True Then
 Secondary_Year_Groups = Sec_YG_Census
 Else If FundingBasis = 2 Then
 Secondary_Year_Groups = Sec_YG_Estimate
 Else Secondary_Year_Groups = 0
 End If 
 Else Exclude(rid, 7698) 
End If
 
 
 
 result = Secondary_Year_Groups
 
 
 
_engine._productResultCache.Add(__key, result)
Return result
End Function


Public Function GetProductResult_8364(rid As String) As Decimal
Dim __key = "8364" + rid
If (_engine._productResultCache.ContainsKey(__key)) = True Then Return _engine._productResultCache(__key)

    Dim result = 0
    
    Dim AcademyFilter As Decimal = GetProductResult_7582(rid) 
    Dim FundingBasis As String = _engine.GetDsDataValue(rid, 11187, "2017181")
     
    Print(AcademyFilter,"AcademyFilter",rid, 8364)
    Print(FundingBasis,"FundingBasis",rid, 8364)
    
    If FundingBasis = "Place" Then
    
    exclude(rid, 8364) 
    
    Else

        If AcademyFilter = 17181 Or AcademyFilter = 17182 Or AcademyFilter = 17183 then
        
        Dim SYG As Decimal = GetProductResult_7698(rid) 
    
        result =   SYG
        
        else
        
        exclude(rid, 8364)
        
        End if
    
    End if
    
    
_engine._productResultCache.Add(__key, result)
Return result
    
End Function


Public Function GetProductResult_8343(rid As String) As Decimal
Dim __key = "8343" + rid
If (_engine._productResultCache.ContainsKey(__key)) = True Then Return _engine._productResultCache(__key)

    Dim result = 0



    Dim AcademyFilter As Decimal = GetProductResult_7582(rid) 
    Dim FundingBasis As String = _engine.GetDsDataValue(rid, 11187, "2017181")
     
    Print(AcademyFilter,"AcademyFilter",rid, 8343)
    Print(FundingBasis,"FundingBasis",rid, 8343)
    
    If FundingBasis = "Place" Then
    
    exclude(rid, 8343)
    
    Else

        If AcademyFilter = 17181 Or AcademyFilter = 17182 Or AcademyFilter = 17183 then
        
        Dim P26_Total_NOR_SBS As Decimal = GetProductResult_7675(rid) 
        Dim P212_PYG As Decimal = GetProductResult_8363(rid) 
        Dim P213_SYG As Decimal = GetProductResult_8364(rid) 
        Dim P194_SparsityPriAveYGSize As Decimal =Divide(P26_Total_NOR_SBS, (P212_PYG+P213_SYG))
           
        print(P26_Total_NOR_SBS,"P26_Total_NOR_SBS",rid, 8343)
        print(P212_PYG,"P212_PYG",rid, 8343)
        print(P213_SYG,"P213_SYG",rid, 8343)
   
            If  P212_PYG = 0 And P213_SYG  = 0 then
            
            result =  0
            
            else
            
            result = P194_SparsityPriAveYGSize
            
            End If 
           
            else
        
            exclude(rid, 8343) 
    
           End if
    
    End if
    
    
result = System.Math.Round(result, 6, MidpointRounding.AwayFromZero)
_engine._productResultCache.Add(__key, result)
Return result
    
End Function


 Public Function GetProductResult_8345(rid As String) As Decimal
Dim __key = "8345" + rid
If (_engine._productResultCache.ContainsKey(__key)) = True Then Return _engine._productResultCache(__key)

    Dim result = 0
        
    Dim AcademyFilter As Decimal = GetProductResult_7582(rid) 
    Dim FundingBasis As String = _engine.GetDsDataValue(rid, 11187, "2017181")
     
    Print(AcademyFilter,"AcademyFilter",rid, 8345)
    Print(FundingBasis,"FundingBasis",rid, 8345)
    
    If FundingBasis = "Place" Then
    
    exclude(rid, 8345) 
    
    Else

        If AcademyFilter = 17181 Or AcademyFilter = 17182 Or AcademyFilter = 17183 then
        Dim P195_SparsityYGThreshold As Decimal = GetProductResult_8344(rid) 
        Dim P194_SparsityAveYGSize   As Decimal = GetProductResult_8343(rid) 
             
        Print(P195_SparsityYGThreshold,"P195_SparsityYGThreshold",rid, 8345)
        Print(P194_SparsityAveYGSize, "P194_SparsityAveYGSize",rid, 8345)
                                                    
            If  P194_SparsityAveYGSize < = P195_SparsityYGThreshold And P194_SparsityAveYGSize > 0 Then 
      
                  Result =    1
      
                  Else 
      
                  Result =    0
                  
                  End If 
        
            Else
            
            exclude(rid, 8345)
            
            End if
        
    End if
    
    
result = System.Math.Round(result, 6, MidpointRounding.AwayFromZero)
_engine._productResultCache.Add(__key, result)
Return result
    
End Function


Public Function GetProductResult_8339(rid As String) As Decimal
Dim __key = "8339" + rid
If (_engine._productResultCache.ContainsKey(__key)) = True Then Return _engine._productResultCache(__key)

    Dim result = 0
    
    Dim AcademyFilter As Decimal = GetProductResult_7582(rid) 
    Dim FundingBasis As String = _engine.GetDsDataValue(rid, 11187, "2017181")
    Dim phase As Decimal = GetProductResult_8333(rid) 
       
    
    Print(AcademyFilter,"AcademyFilter",rid, 8339)
    Print(FundingBasis,"FundingBasis",rid, 8339)
    
    If FundingBasis = "Place" Then
    
    exclude(rid, 8339)
    
    Else

        If AcademyFilter = 17181 Or AcademyFilter = 17182 Or AcademyFilter = 17183 then
        
       
        Dim SparsityPriUnit As Decimal = latoprov(_engine.GetLaDataValue(rid, 86646, Nothing))
        Dim SparsityMidUnit As Decimal = latoprov(_engine.GetLaDataValue(rid, 86650, Nothing))
        Dim SparsitySecUnit As Decimal = latoprov(_engine.GetLaDataValue(rid, 86648, Nothing))
        Dim SparsityAllUnit As Decimal = latoprov(_engine.GetLaDataValue(rid, 86652, Nothing))

        Print(phase,"phase",rid, 8339)
        Print(SparsityPriUnit,"SparsityPriUnit",rid, 8339)
        Print(SparsityMidUnit,"SparsityMidUnit",rid, 8339)
        Print(SparsitySecUnit,"SparsitySecUnit",rid, 8339)
        Print(SparsityAllUnit,"SparsityAllUnit",rid, 8339)
            
            If phase = 1 then
            
            result =   SparsityPriUnit
            
                ElseIf phase = 2 Or phase = 4 then
                
                result = SparsityMidUnit
                
                    ElseIf phase = 3 then
                    
                    result = SparsitySecUnit
                        
                        ElseIf phase = 5 then
                        
                        result = SparsityAllUnit
                        
                        Else  
                        
                        result = 0
                        End If
                                      
                    else
                    
                    exclude(rid, 8339)
            
            End if
    
    End if
    
    
result = System.Math.Round(result, 6, MidpointRounding.AwayFromZero)
_engine._productResultCache.Add(__key, result)
Return result

End Function


Public Function GetProductResult_8346(rid As String) As Decimal
Dim __key = "8346" + rid
If (_engine._productResultCache.ContainsKey(__key)) = True Then Return _engine._productResultCache(__key)

    Dim result = 0
    
    Dim AcademyFilter As Decimal = GetProductResult_7582(rid) 
    Dim FundingBasis As String = _engine.GetDsDataValue(rid, 11187, "2017181")
    
    Dim Phase As Decimal = GetProductResult_8333(rid) 
    Dim P186_SparsityTaperFlagPri As Decimal = GetProductResult_8334(rid) 
    Dim P187_SparsityTaperFlagMid As Decimal = GetProductResult_8335(rid) 
    Dim P188_SparsityTaperFlagSec As Decimal = GetProductResult_8336(rid) 
    Dim P189_SparsityTaperFlagAllThru As Decimal = GetProductResult_8337(rid) 
    Dim P193_SparsityDistMet As Decimal = GetProductResult_8342(rid) 
    Dim P196_SparsityYGThresholdMet_YN As Decimal = GetProductResult_8345(rid) 
    Dim P190_SparsityUnit As Decimal = GetProductResult_8339(rid) 
    Dim SparsityLumpSumAPT As Decimal = _engine.GetDsDataValue(rid, 86146)

    Print(AcademyFilter, "AcademyFilter", rid, 8346)
    Print(FundingBasis,"FundingBasis",rid, 8346)
    Print(Phase,"Phase",rid, 8346)    
    Print(P186_SparsityTaperFlagPri, "P186_SparsityTaperFlagPri",rid, 8346)
    Print(P187_SparsityTaperFlagMid, "P187_SparsityTaperFlagMid",rid, 8346)
    Print(P188_SparsityTaperFlagSec, "P188_SparsityTaperFlagSec",rid, 8346)
    Print(P189_SparsityTaperFlagAllThru, "P189_SparsityTaperFlagAllThru",rid, 8346)    
    Print(P193_SparsityDistMet, "P193_SparsityDistMet",rid, 8346)
    Print(P196_SparsityYGThresholdMet_YN, "P196_SparsityYGThresholdMet_YN",rid, 8346)
    Print(P190_SparsityUnit, "P190_SparsityUnit",rid, 8346)    
    Print(SparsityLumpSumAPT,"SparsityLumpSumAPT",rid, 8346)
    
     
    If FundingBasis = "Place" Then
    
    exclude(rid, 8346) 
    
    Else
    
        If AcademyFilter = 17181 Or (AcademyFilter = 17182 And FundingBasis = "Estimate") Or (AcademyFilter = 17183 And FundingBasis = "Estimate")  then
            
              If Phase = 1 And  P186_SparsityTaperFlagPri = 0 And  P193_SparsityDistMet = 1 And P196_SparsityYGThresholdMet_YN = 1 then
                       
              result = P190_SparsityUnit
              
              Else
                           
              result = 0

                       
                  If (Phase = 2 Or Phase = 4) And  P187_SparsityTaperFlagMid = 0 And  P193_SparsityDistMet = 1 And P196_SparsityYGThresholdMet_YN = 1 then
                           
                  result = P190_SparsityUnit
                  
                  Else
                               
                  result = 0
    
                           
                      If Phase = 3 And  P188_SparsityTaperFlagSec = 0 And  P193_SparsityDistMet = 1 And P196_SparsityYGThresholdMet_YN = 1 then
                               
                      result = P190_SparsityUnit
                      
                      Else
                                   
                      result = 0
      
                       
                          If Phase = 5 And  P189_SparsityTaperFlagAllThru = 0 And  P193_SparsityDistMet = 1 And P196_SparsityYGThresholdMet_YN = 1 then
                                   
                          result = P190_SparsityUnit
                          
                          Else
                                       
                          result = 0
                                   
                          End if
                      
                      End If
                  
                  End If
              
              End If
    
                ElseIf (AcademyFilter = 17182 And FundingBasis = "Census") Or (AcademyFilter = 17183 And FundingBasis = "Census")  then
    
                Result =  SparsityLumpSumAPT
                
                Else
                
                Exclude(rid, 8346)
                
                End If
   
   End If
   
   
result = System.Math.Round(result, 6, MidpointRounding.AwayFromZero)
_engine._productResultCache.Add(__key, result)
Return result
   
End Function


Public Function GetProductResult_8349(rid As String) As Decimal
Dim __key = "8349" + rid
If (_engine._productResultCache.ContainsKey(__key)) = True Then Return _engine._productResultCache(__key)

    Dim result = 0
    
    Dim AcademyFilter As Decimal = GetProductResult_7582(rid) 
    Dim FundingBasis As String = _engine.GetDsDataValue(rid, 11187, "2017181")
     
    Print(AcademyFilter,"AcademyFilter",rid, 8349)
    Print(FundingBasis,"FundingBasis",rid, 8349)
    
    If FundingBasis = "Place" Then
    
    exclude(rid, 8349) 
    
    Else

        If AcademyFilter = 17181 Or AcademyFilter = 17182 Or AcademyFilter = 17183 then

        Dim P197_SparsityLumpSumSubtotal As Decimal = GetProductResult_8346(rid) 
        Dim Days_Open As Decimal = GetProductResult_8151(rid) 
        Dim Year_Days As Decimal = 365 
                                
        Result = (P197_SparsityLumpSumSubtotal) *Divide(Days_Open, Year_Days)
    
        Else   
        
        Exclude(rid, 8349)  
        
        End If  
    
    End if
   
    
result = System.Math.Round(result, 5, MidpointRounding.AwayFromZero)
_engine._productResultCache.Add(__key, result)
Return result
    
End Function


 Public Function GetProductResult_8347(rid As String) As Decimal
Dim __key = "8347" + rid
If (_engine._productResultCache.ContainsKey(__key)) = True Then Return _engine._productResultCache(__key)

    Dim result = 0
    
    Dim AcademyFilter As Decimal = GetProductResult_7582(rid) 
    Dim FundingBasis As String = _engine.GetDsDataValue(rid, 11187, "2017181")
    Dim Phase As Decimal = GetProductResult_8333(rid) 
    
    Dim P186_SparsityTaperFlagPri As Decimal =  GetProductResult_8334(rid) 
    Dim P187_SparsityTaperFlagMid As Decimal =  GetProductResult_8335(rid) 
    Dim P188_SparsityTaperFlagSec As Decimal =  GetProductResult_8336(rid) 
    Dim P189_SparsityTaperFlagAllThru As Decimal =  GetProductResult_8337(rid) 
    Dim P193_SparsityDistMet As Decimal = GetProductResult_8342(rid) 
    Dim P196_SparsityYGThresholdMet_YN As Decimal = GetProductResult_8345(rid) 
    Dim P190_SparsityUnit As Decimal = GetProductResult_8339(rid) 
    Dim P194_SparsityAveYGSize As Decimal = GetProductResult_8343(rid) 
    Dim P195_SparsityYGThreshold As Decimal = GetProductResult_8344(rid) 
    Dim TaperCalc As Decimal =  P190_SparsityUnit * (1- (Divide(P194_SparsityAveYGSize, P195_SparsityYGThreshold)))

    Print(AcademyFilter,"AcademyFilter",rid, 8347)
    Print(Phase,"Phase",rid, 8347)
    Print(P186_SparsityTaperFlagPri, "P186_SparsityTaperFlagPri",rid, 8347)
    Print(P187_SparsityTaperFlagMid, "P187_SparsityTaperFlagMid",rid, 8347)
    Print(P188_SparsityTaperFlagSec, "P188_SparsityTaperFlagSec",rid, 8347)
    Print(P189_SparsityTaperFlagAllThru, "P189_SparsityTaperFlagAllThru",rid, 8347)   
    Print(P193_SparsityDistMet, "P193_SparsityDistMet",rid, 8347)
    Print(P196_SparsityYGThresholdMet_YN, "P196_SparsityYGThresholdMet_YN",rid, 8347)
    Print(P190_SparsityUnit, "P190_SparsityUnit",rid, 8347)
    Print(P194_SparsityAveYGSize, "P194_SparsityAveYGSize",rid, 8347)
    Print(P195_SparsityYGThreshold, "P195_SparsityYGThreshold",rid, 8347)
        
    If FundingBasis = "Place" Then
    
    exclude(rid, 8347) 
    
    Else
          
        If AcademyFilter = 17181 Or (AcademyFilter = 17182 And FundingBasis = "Estimate") Or (AcademyFilter = 17183 And FundingBasis = "Estimate")   then
              
            If  Phase = 1 And P186_SparsityTaperFlagPri = 1 And P193_SparsityDistMet = 1 And P196_SparsityYGThresholdMet_YN =1 then   
                                           
            result =  TaperCalc
              
            Else
                                 
            result =  0
                            
                If  (Phase = 2 Or Phase = 4) And P187_SparsityTaperFlagMid = 1 And P193_SparsityDistMet = 1 And P196_SparsityYGThresholdMet_YN =1 then   
                                                           
                result =  TaperCalc
                      
                Else
                                         
                result =  0
                                      
                    If  Phase = 3 And P188_SparsityTaperFlagSec = 1 And P193_SparsityDistMet = 1 And P196_SparsityYGThresholdMet_YN =1 then   
                                                                           
                    result =  TaperCalc
                              
                    Else
                                                 
                    result =  0
                                                       
                        If  Phase = 5 And P189_SparsityTaperFlagAllThru = 1 And P193_SparsityDistMet = 1 And P196_SparsityYGThresholdMet_YN =1 then   
                                                                                   
                        result =  TaperCalc
                                  
                        Else
                                                     
                        result =  0  
                                    
                        End If
                    
                    End If
                
                End If        
            
            End If
        
            ElseIf (AcademyFilter = 17182 And FundingBasis = "Census") Or (AcademyFilter = 17183 And FundingBasis = "Census")  then
        
            Result =  0
            
            Else
            
            Exclude(rid, 8347)
            
            End If
        
        End If          
       
        
result = System.Math.Round(result, 6, MidpointRounding.AwayFromZero)
_engine._productResultCache.Add(__key, result)
Return result
        
End Function


Public Function GetProductResult_8350(rid As String) As Decimal
Dim __key = "8350" + rid
If (_engine._productResultCache.ContainsKey(__key)) = True Then Return _engine._productResultCache(__key)

    Dim result = 0
    
    Dim AcademyFilter As Decimal = GetProductResult_7582(rid) 
    Dim FundingBasis As String = _engine.GetDsDataValue(rid, 11187, "2017181")
     
    Print(AcademyFilter,"AcademyFilter",rid, 8350)
    Print(FundingBasis,"FundingBasis",rid, 8350)
    
    If FundingBasis = "Place" Then
    
    exclude(rid, 8350) 
    
    Else

        If AcademyFilter = 17181 Or AcademyFilter = 17182 Or AcademyFilter = 17183 then

        Dim P198_SparsityTaperSubtotal As Decimal = GetProductResult_8347(rid) 
        Dim Days_Open As Decimal = GetProductResult_8151(rid) 
        Dim Year_Days As Decimal = 365 
                                
        Result = (P198_SparsityTaperSubtotal) *Divide(Days_Open, Year_Days)
    
        Else   
        
        Exclude(rid, 8350)  
        
        End If  
    
    End if
    
    
result = System.Math.Round(result, 6, MidpointRounding.AwayFromZero)
_engine._productResultCache.Add(__key, result)
Return result
    
End Function


Public Function GetProductResult_8452(rid As String) As Decimal
Dim __key = "8452" + rid
If (_engine._productResultCache.ContainsKey(__key)) = True Then Return _engine._productResultCache(__key)

    Dim result = 0
    
    Dim AcademyFilter As Decimal = GetProductResult_7582(rid) 
    Dim FundingBasis As String = _engine.GetDsDataValue(rid, 11187, "2017181")
     
    Print(AcademyFilter,"AcademyFilter",rid, 8452)
    Print(FundingBasis,"FundingBasis",rid, 8452)
    
    If FundingBasis = "Place" Then
    
    exclude(rid, 8452) 
    
    Else

    If AcademyFilter = 17181 Or AcademyFilter = 17182 Or AcademyFilter = 17183 then
  
    Dim InYearPriBESubtotal As Decimal = GetProductResult_8156(rid) 
    Dim InYearKS3_BESubtotal As Decimal = GetProductResult_8162(rid) 
    Dim InYearKS4_BESubtotal As Decimal = GetProductResult_8167(rid) 
    Dim InYearPriFSMSubtotal As Decimal = GetProductResult_8171(rid) 
    Dim InYearPriFSM6Subtotal As Decimal = GetProductResult_8177(rid) 
    Dim InYearSecFSMSubtotal As Decimal = GetProductResult_8181(rid) 
    Dim InYearSecFSM6Subtotal As Decimal = GetProductResult_8187(rid) 
    Dim InYearIDACI1PriSubtotal As Decimal = GetProductResult_8193(rid) 
    Dim InYearIDACI2PriSubtotal As Decimal = GetProductResult_8199(rid) 
    Dim InYearIDACI3PriSubtotal As Decimal = GetProductResult_8205(rid) 
    Dim InYearIDACI4PriSubtotal As Decimal = GetProductResult_8211(rid) 
    Dim InYearIDACI5PriSubtotal As Decimal = GetProductResult_8217(rid) 
    Dim InYearIDACI6PriSubtotal As Decimal = GetProductResult_8223(rid) 
    Dim InYearIDACI1SecSubtotal As Decimal = GetProductResult_8229(rid) 
    Dim InYearIDACI2SecSubtotal As Decimal = GetProductResult_8235(rid) 
    Dim InYearIDACI3SecSubtotal As Decimal = GetProductResult_8241(rid) 
    Dim InYearIDACI4SecSubtotal As Decimal = GetProductResult_8247(rid) 
    Dim InYearIDACI5SecSubtotal As Decimal = GetProductResult_8253(rid) 
    Dim InYearIDACI6SecSubtotal As Decimal = GetProductResult_8259(rid) 
    Dim InYearLACSubtotal As Decimal = GetProductResult_8265(rid) 
    Dim InYearPPATotalFunding As Decimal = GetProductResult_8283(rid) 
    Dim InYearSecPASubtotal As Decimal = GetProductResult_8290(rid) 
    Dim InYearEAL1PriSubtotal As Decimal = GetProductResult_8294(rid) 
    Dim InYearEAL2PriSubtotal As Decimal = GetProductResult_8298(rid) 
    Dim InYearEAL3PriSubtotal As Decimal = GetProductResult_8304(rid) 
    Dim InYearEAL1SecSubtotal As Decimal = GetProductResult_8308(rid) 
    Dim InYearEAL2SecSubtotal As Decimal = GetProductResult_8312(rid) 
    Dim InYearEAL3SecSubtotal As Decimal = GetProductResult_8318(rid) 
    Dim InYearMobPriSubtotal As Decimal = GetProductResult_8325(rid) 
    Dim InYearMobSecSubtotal As Decimal = GetProductResult_8332(rid) 
    Dim InYearSparsityPriLumpSumSubtotal As Decimal = GetProductResult_8349(rid) 
    Dim InYearSparsityPriTaperSubtotal As Decimal = GetProductResult_8350(rid) 
    Dim InYearTotalPupilLedFactors As Decimal =  InYearPriBESubtotal + InYearKS3_BESubtotal + InYearKS4_BESubtotal + InYearPriFSMSubtotal + InYearPriFSM6Subtotal + InYearSecFSMSubtotal + InYearSecFSM6Subtotal +  
                                     InYearIDACI1PriSubtotal + InYearIDACI2PriSubtotal + InYearIDACI3PriSubtotal + InYearIDACI4PriSubtotal + InYearIDACI5PriSubtotal + InYearIDACI6PriSubtotal +
                                     InYearIDACI1SecSubtotal + InYearIDACI2SecSubtotal + InYearIDACI3SecSubtotal + InYearIDACI4SecSubtotal + InYearIDACI5SecSubtotal + InYearIDACI6SecSubtotal +
                                     InYearLACSubtotal + InYearPPATotalFunding + InYearSecPASubtotal + InYearEAL1PriSubtotal + InYearEAL2PriSubtotal + InYearEAL3PriSubtotal + InYearEAL1SecSubtotal +
                                     InYearEAL2SecSubtotal + InYearEAL3SecSubtotal + InYearMobPriSubtotal + InYearMobSecSubtotal 
    
    Print(InYearPriBESubtotal,"P007_InYearPriBESubtotal",rid, 8452)
    Print(InYearKS3_BESubtotal,"P012_InYearKS3_BESubtotal",rid, 8452)
    Print(InYearKS4_BESubtotal,"P018_InYearKS4_BESubtotal",rid, 8452)
    Print(InYearPriFSMSubtotal,"P023_InYearPriFSMSubtotal",rid, 8452)
    Print(InYearPriFSM6Subtotal,"P029_InYearPriFSM6Subtotal",rid, 8452)
    Print(InYearSecFSMSubtotal,"P034_InYearSecFSMSubtotal",rid, 8452)
    Print(InYearSecFSM6Subtotal,"P040_InYearSecFSM6Subtotal",rid, 8452)
    Print(InYearIDACI1PriSubtotal,"P046_InYearIDACI1PriSubtotal",rid, 8452)
    Print(InYearIDACI2PriSubtotal,"P052_InYearIDACI2PriSubtotal",rid, 8452)
    Print(InYearIDACI3PriSubtotal,"P058_InYearIDACI3PriSubtotal",rid, 8452)
    Print(InYearIDACI4PriSubtotal,"P064_InYearIDACI4PriSubtotal",rid, 8452)
    Print(InYearIDACI5PriSubtotal,"P070_InYearIDACI5PriSubtotal",rid, 8452)
    Print(InYearIDACI6PriSubtotal,"P076_InYearIDACI6PriSubtotal",rid, 8452)
    Print(InYearIDACI1SecSubtotal,"P082_InYearIDACI1SecSubtotal",rid, 8452)
    Print(InYearIDACI2SecSubtotal,"P088_InYearIDACI2SecSubtotal",rid, 8452)
    Print(InYearIDACI3SecSubtotal,"P094_InYearIDACI3SecSubtotal",rid, 8452)
    Print(InYearIDACI4SecSubtotal,"P100_InYearIDACI4SecSubtotal",rid, 8452)
    Print(InYearIDACI5SecSubtotal,"P106_InYearIDACI5SecSubtotal",rid, 8452)
    Print(InYearIDACI6SecSubtotal,"P112_InYearIDACI6SecSubtotal",rid, 8452)
    Print(InYearLACSubtotal,"P119_InYearLACSubtotal",rid, 8452)
    Print(InYearPPATotalFunding,"P135_InYearPPATotalFunding",rid, 8452)
    Print(InYearSecPASubtotal,"P141_InYearSecPASubtotal",rid, 8452)
    Print(InYearEAL1PriSubtotal,"P146_InYearEAL1PriSubtotal",rid, 8452)
    Print(InYearEAL2PriSubtotal,"P151_InYearEAL2PriSubtotal",rid, 8452)
    Print(InYearEAL3PriSubtotal,"P157_InYearEAL3PriSubtotal",rid, 8452)
    Print(InYearEAL1SecSubtotal,"P162_InYearEAL1SecSubtotal",rid, 8452)
    Print(InYearEAL2SecSubtotal,"P167_InYearEAL2SecSubtotal",rid, 8452)
    Print(InYearEAL3SecSubtotal,"P173_InYearEAL3SecSubtotal",rid, 8452)
    Print(InYearMobPriSubtotal,"P179_InYearMobPriSubtotal",rid, 8452)
    Print(InYearMobSecSubtotal,"P185_InYearMobSecSubtotal",rid, 8452)
    Print(InYearSparsityPriLumpSumSubtotal,"P199_InYearSparsityPriLumpSumSubtotal",rid, 8452)
    Print(InYearSparsityPriTaperSubtotal,"P200_InYearSparsityPriTaperSubtotal",rid, 8452)
     
    Result = InYearTotalPupilLedFactors
    
    Else
    
    exclude(rid, 8452)
    
    End if
    
    End if
    
    
result = System.Math.Round(result, 6, MidpointRounding.AwayFromZero)
_engine._productResultCache.Add(__key, result)
Return result
    
End Function


 Public Function GetProductResult_7730(rid As String) As Decimal
Dim __key = "7730" + rid
If (_engine._productResultCache.ContainsKey(__key)) = True Then Return _engine._productResultCache(__key)

 Dim result As Decimal = 0
 
 Dim Confirmed_Open As Decimal = GetProductResult_7708(rid) 
 Dim SBSPupilLed As Decimal = GetProductResult_8452(rid) 
 

 If Confirmed_Open = 1 Then 
 Result = SBSPupilLed
 
 Else Exclude(rid, 7730)
 End If 
 
 
 
 
_engine._productResultCache.Add(__key, result)
Return result
End Function

Private _engine As CalculationEngine
Public Sub SetEngine(engine As CalculationEngine)
    _engine = engine
End Sub

Public Function Sum(dsId As Integer, Optional filter As String = Nothing) As Decimal
    Return _engine.Sum(dsId, filter)
End Function

Public Function Sum(dataTable As Object, productId As Integer, Optional filterFunc As CalculationEngine.ProductFilterFunc = Nothing) As Decimal
    Return _engine.Sum(dataTable, productId, filterFunc)
End Function

Public Function Sum(dataTable As Object, tableName As String, colName As String, Optional filterFunc As CalculationEngine.ProductFilterFunc = Nothing) As Decimal
    Return _engine.Sum(dataTable, tableName, colName, filterFunc)
End Function

Public Function Min(dsId As Integer, Optional filter As String = Nothing) As Decimal
    Return _engine.Min(dsId, filter)
End Function

Public Function Min(dataTable As Object, productId As Integer, Optional filterFunc As CalculationEngine.ProductFilterFunc = Nothing) As Decimal
    Return _engine.Min(dataTable, productId, filterFunc)
End Function

Public Function Min(dataTable As Object, tableName As String, colName As String, Optional filterFunc As CalculationEngine.ProductFilterFunc = Nothing) As Decimal
    Return _engine.Min(dataTable, tableName, colName, filterFunc)
End Function

Public Function Max(dsId As Integer, Optional filter As String = Nothing) As Decimal
    Return _engine.Max(dsId, filter)
End Function

Public Function Max(dataTable As Object, productId As Integer, Optional filterFunc As CalculationEngine.ProductFilterFunc = Nothing) As Decimal
    Return _engine.Max(dataTable, productId, filterFunc)
End Function

Public Function Max(dataTable As Object, tableName As String, colName As String, Optional filterFunc As CalculationEngine.ProductFilterFunc = Nothing) As Decimal
    Return _engine.Max(dataTable, tableName, colName, filterFunc)
End Function

Public Function Avg(dsId As Integer, Optional filter As String = Nothing) As Decimal
    Return _engine.Avg(dsId, filter)
End Function

Public Function Avg(dataTable As Object, productId As Integer, Optional filterFunc As CalculationEngine.ProductFilterFunc = Nothing) As Decimal
    Return _engine.Avg(dataTable, productId, filterFunc)
End Function

Public Function Avg(dataTable As Object, tableName As String, colName As String, Optional filterFunc As CalculationEngine.ProductFilterFunc = Nothing) As Decimal
    Return _engine.Avg(dataTable, tableName, colName, filterFunc)
End Function

Public Function Count(dsId As Integer, Optional filter As String = Nothing) As Integer
    Return _engine.Count(dsId, filter)
End Function

Public Function Count(dataTable As Object, productId As Integer, Optional filterFunc As CalculationEngine.ProductFilterFunc = Nothing) As Decimal
    Return _engine.Count(dataTable, productId, filterFunc)
End Function

Public Function Count(dataTable As Object, tableName As String, colName As String, Optional filterFunc As CalculationEngine.ProductFilterFunc = Nothing) As Decimal
    Return _engine.Count(dataTable, tableName, colName, filterFunc)
End Function

Public Sub Print(value As Object, name As String, rid As String, Optional productId As Integer = 0)
    _engine.Print(value, name, rid, productId)
End Sub

Public Sub Exclude(rid As String, productId As Integer)
    _engine.Exclude(rid, productId)
End Sub

Public Sub Exclude(productId As Integer, ByVal ParamArray rids() As String)
    _engine.Exclude(productId, rids)
End Sub

Public Function Predecessor(rid As String, dsId As Integer, calculationFunction As String, predecessorDsStatus As string) As Decimal
    Return _engine.Predecessor(rid, dsId, calculationFunction, predecessorDsStatus)
End Function

Public Function PredData(rid As String, dsId As Integer, calculationFunction As String, predecessorDsStatus As string) As Decimal
    Return _engine.PredData(rid, dsId, calculationFunction, predecessorDsStatus)
End Function

Public Function PredProd(rid As String, product As String, calculationFunction As String) As Decimal
    Return _engine.PredProd(rid, product, calculationFunction)
End Function

Public Function IIf(value As Object, falsePart As Object) As Object
    Return _engine.IIf(value, falsePart)
End Function

Public Function IIf(value As Object, truePart As Object, falsePart As Object) As Object
    Return _engine.IIf(value, truePart, falsePart)
End Function

Public Function LaToProv(value As Object) As Object
    Return _engine.LaToProv(value)
End Function

Public Function GetProductResults(productId As Integer) As DataTable
    Return _engine.GetProductResults(productId)
End Function

Private Function Divide(val1 As Object, val2 As Object) As Decimal
If (val1 Is Nothing OrElse val2 Is Nothing) Then Return Nothing
Dim conv1 As IConvertible = TryCast(val1, IConvertible)
Dim conv2 As IConvertible = TryCast(val2, IConvertible)
' Avoid dividing by zero if the type is integer or decimal
If (conv1 IsNot Nothing AndAlso conv2 IsNot Nothing) Then
    If (conv2.GetTypeCode() = TypeCode.Int32 OrElse conv2.GetTypeCode() = TypeCode.Decimal) Then
        If (conv2.ToInt64(Nothing) = 0) Then
            Return Nothing
        End If
    End If
End If

    Return val1 / val2
End Function

Public ReadOnly Property CurrentScenario() As ScenarioData
    Get
        Return _engine.Scenario
    End Get
End Property

End Module
