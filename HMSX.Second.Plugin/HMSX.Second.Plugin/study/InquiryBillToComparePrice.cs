using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using HMSX.Second.Plugin.study;
using Kingdee.BOS;
using Kingdee.BOS.App;
using Kingdee.BOS.App.Data;
using Kingdee.BOS.Contracts;
using Kingdee.BOS.Core;
using Kingdee.BOS.Core.Metadata;
using Kingdee.BOS.Core.Metadata.ConvertElement;
using Kingdee.BOS.Core.Metadata.ConvertElement.PlugIn;
using Kingdee.BOS.Core.Metadata.ConvertElement.PlugIn.Args;
using Kingdee.BOS.Core.Metadata.EntityElement;
using Kingdee.BOS.Core.Metadata.FieldElement;
using Kingdee.BOS.Core.Metadata.QueryElement;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.Resource;
using Kingdee.K3.SCM.App.Utils;

namespace Kingdee.K3.SCM.App.SVM.ServicePlugIn
{
    // Token: 0x02000011 RID: 17
    [Kingdee.BOS.Util.HotUpdate]
    [Description("SVM：询价单to比价单")]
    public class InquiryBillToComparePrice : AbstractConvertPlugIn
    {
        // Token: 0x06000066 RID: 102 RVA: 0x000084C0 File Offset: 0x000066C0
        public override void OnQueryBuilderParemeter(QueryBuilderParemeterEventArgs e)
        {
            e.SelectItems.Add(new SelectorItemInfo("Fbillno"));
        }

        // Token: 0x06000067 RID: 103 RVA: 0x000084D8 File Offset: 0x000066D8
        public override void OnParseFilter(ParseFilterEventArgs e)
        {
            SelectField selectField = e.SourceBusinessInfo.GetQueryInfo().GetSelectField("Fbillno");
            if (selectField != null)
            {
                string text = selectField.FullFieldName.Substring(0, selectField.FullFieldName.IndexOf('.'));
                if (text != null && text.Length > 0)
                {
                    string text2 = string.Format(" exists (select 1 from t_svm_inquirysup tluossup where tluossup.fid={0}.fid and tluossup.fquotestatus<>'A' ) ", text);
                    if (!string.IsNullOrWhiteSpace(e.FilterPolicySQL))
                    {
                        e.FilterPolicySQL = e.FilterPolicySQL + " and  " + text2;
                    }
                    else
                    {
                        e.FilterPolicySQL = text2;
                    }
                    string text3 = ResManager.LoadKDString("单据必须已报价！", "00444297030008930", SubSystemType.SCM, new object[0]);
                    if (!string.IsNullOrWhiteSpace(e.PlugFilterDesc))
                    {
                        e.PlugFilterDesc += text3;
                        return;
                    }
                    e.PlugFilterDesc = text3;
                }
            }
        }

        // Token: 0x06000068 RID: 104 RVA: 0x000085A1 File Offset: 0x000067A1
        public override void OnGetDrawSourceData(GetDrawSourceDataEventArgs e)
        {
            this.GetQuoteDataSourceData(e.SourceData);
        }

        // Token: 0x06000069 RID: 105 RVA: 0x000085AF File Offset: 0x000067AF
        public override void OnGetSourceData(GetSourceDataEventArgs e)
        {
            this.GetQuoteDataSourceData(e.SourceData);
        }

        // Token: 0x0600006A RID: 106 RVA: 0x000085C0 File Offset: 0x000067C0
        public override void OnAfterCreateLink(CreateLinkEventArgs e)
        {
            ExtendedDataEntity[] array = e.TargetExtendedDataEntities.FindByEntityKey("FCompareEntry");
            if (array == null || array.Length == 0)
            {
                return;
            }
            this.AddQuoteRows(e, array);
            this.AddQuoteSumRows(e);
        }

        // Token: 0x0600006B RID: 107 RVA: 0x000085F8 File Offset: 0x000067F8
        //总金额
        public virtual void AddQuoteSumRows(CreateLinkEventArgs e)
        {
            ExtendedDataEntity[] array = e.TargetExtendedDataEntities.FindByEntityKey("FBillHead");
            if (array == null || array.Length == 0)
            {
                return;
            }
            BaseDataField field = e.TargetBusinessInfo.GetField("FSumCurr") as BaseDataField;
            BaseDataField field2 = e.TargetBusinessInfo.GetField("FSumSupplier") as BaseDataField;
            foreach (ExtendedDataEntity extendedDataEntity in array)
            {
                string a = Convert.ToString(extendedDataEntity["ComMode"]);
                if (a == "SumAmount")
                {
                    DynamicObjectCollection dynamicObjectCollection = extendedDataEntity.DataEntity["QuoteSum"] as DynamicObjectCollection;
                    if (dynamicObjectCollection == null)
                    {
                        extendedDataEntity.DataEntity["QuoteEntry"] = new DynamicObjectCollection(dynamicObjectCollection.DynamicCollectionItemPropertyType, null);
                        dynamicObjectCollection = extendedDataEntity.DataEntity["QuoteEntry"] as DynamicObjectCollection;
                    }
                    else
                    {
                        dynamicObjectCollection.Clear();
                    }
                    int num = 1;
                    foreach (KeyValuePair<long, QuoteSumData> keyValuePair in this.QuoteSumDatas)
                    {
                        DynamicObject dynamicObject = new DynamicObject(dynamicObjectCollection.DynamicCollectionItemPropertyType);
                        dynamicObject["Seq"] = num++;
                        dynamicObject["SumQuoteNO"] = keyValuePair.Value.QuoteBillNO;
                        dynamicObject["SumBillAmount"] = keyValuePair.Value.BillAmount;
                        dynamicObject["SumBillAllAmount"] = keyValuePair.Value.BillAllAmount;
                        dynamicObject["SumQuoteId"] = keyValuePair.Value.QuoteBillId;
                        dynamicObject["SumComRst"] = keyValuePair.Value.SumComRst;
                        dynamicObject["SumSupplier_id"] = keyValuePair.Value.SupplierId;
                        FieldUtils.SetBaseDataFieldValue(base.Context, field2, dynamicObject, keyValuePair.Value.SupplierId);
                        dynamicObject["SumCurr_id"] = keyValuePair.Value.CurrencyId;
                        FieldUtils.SetBaseDataFieldValue(base.Context, field, dynamicObject, keyValuePair.Value.CurrencyId);
                        dynamicObjectCollection.Add(dynamicObject);
                    }
                }
            }
        }

        // Token: 0x0600006C RID: 108 RVA: 0x00008884 File Offset: 0x00006A84
        public virtual void AddQuoteRows(CreateLinkEventArgs e, ExtendedDataEntity[] entrys)
        {
            if (this.QuoteDatas == null || this.QuoteDatas.Count == 0)
            {
                return;
            }
            Entity entity = e.TargetBusinessInfo.GetEntity("FQuoteEntry");
            BaseDataField field = e.TargetBusinessInfo.GetField("FQuoteUnitID") as BaseDataField;
            BaseDataField field2 = e.TargetBusinessInfo.GetField("FQuoteBaseUnitId") as BaseDataField;
            BaseDataField field3 = e.TargetBusinessInfo.GetField("FQuoteSupplier") as BaseDataField;
            e.TargetBusinessInfo.GetField("FQuoteContact");
            BaseDataField field4 = e.TargetBusinessInfo.GetField("FQuoteCurrencyId") as BaseDataField;
            foreach (ExtendedDataEntity extendedDataEntity in entrys)
            {
                bool flag = false;
                DynamicObject dynamicObject = extendedDataEntity.DataEntity.Parent as DynamicObject;
                if (dynamicObject != null)
                {
                    flag = Convert.ToString(dynamicObject["ComMode"]) == "SumAmount";
                }
                DynamicObjectCollection dynamicObjectCollection = extendedDataEntity.DataEntity["QuoteEntry"] as DynamicObjectCollection;
                if (dynamicObjectCollection == null)
                {
                    extendedDataEntity.DataEntity["QuoteEntry"] = new DynamicObjectCollection(entity.DynamicObjectType, null);
                    dynamicObjectCollection = extendedDataEntity.DataEntity["QuoteEntry"] as DynamicObjectCollection;
                }
                else
                {
                    dynamicObjectCollection.Clear();
                }
                foreach (DynamicObject dynamicObject2 in ((DynamicObjectCollection)extendedDataEntity.DataEntity["FCompareEntry_Link"]))
                {
                    long key = Convert.ToInt64(dynamicObject2["SId"]);
                    List<QuoteData> list = null;
                    if (this.QuoteDatas.ContainsKey(key))
                    {
                        list = (from p in this.QuoteDatas[key]
                                orderby p.TaxPrice
                                select p).ToList<QuoteData>();
                    }
                    if (list != null && list.Count != 0)
                    {
                        bool flag2 = true;
                        int num = 1;
                        foreach (QuoteData quoteData in list)
                        {
                            DynamicObject dynamicObject3 = new DynamicObject(entity.DynamicObjectType);
                            dynamicObject3["Seq"] = num++;
                            dynamicObject3["QuoteBillNo"] = quoteData.BillNO;
                            dynamicObject3["QuoteDate"] = quoteData.Date;
                            dynamicObject3["QuoteQty"] = quoteData.Qty;
                            dynamicObject3["QuoteBaseQty"] = quoteData.BaseQty;
                            dynamicObject3["QuotePrice"] = quoteData.Price;
                            dynamicObject3["QuoteTaxPrice"] = quoteData.TaxPrice;
                            dynamicObject3["TaxRate"] = quoteData.TaxRate;
                            dynamicObject3["QuoteComfirmQty"] = quoteData.Qty;
                            dynamicObject3["QuoteComfirmBaseQty"] = quoteData.BaseQty;
                            dynamicObject3["QuoteComfirmPrice"] = quoteData.Price;
                            dynamicObject3["QuoteComfirmTaxPrice"] = quoteData.TaxPrice;
                            dynamicObject3["QuoteBillEntryId"] = quoteData.QuoteBillEntryId;
                            dynamicObject3["QuoteSupplier_id"] = quoteData.SupplierId;
                            FieldUtils.SetBaseDataFieldValue(base.Context, field3, dynamicObject3, quoteData.SupplierId);
                            dynamicObject3["QuoteContacter"] = quoteData.Contact;
                            dynamicObject3["QuoteUnitID_id"] = quoteData.UnitId;
                            FieldUtils.SetBaseDataFieldValue(base.Context, field, dynamicObject3, quoteData.UnitId);
                            dynamicObject3["QuoteBaseUnitId_id"] = quoteData.BaseUnitId;
                            FieldUtils.SetBaseDataFieldValue(base.Context, field2, dynamicObject3, quoteData.BaseUnitId);
                            dynamicObject3["QuoteCurrencyId_id"] = quoteData.CurrencyId;
                            FieldUtils.SetBaseDataFieldValue(base.Context, field4, dynamicObject3, quoteData.CurrencyId);
                            if (!flag)
                            {
                                if (flag2)
                                {
                                    dynamicObject3["QuoteResult"] = "1";
                                    flag2 = false;
                                }
                                else
                                {
                                    dynamicObject3["QuoteResult"] = "9";
                                }
                            }
                            else
                            {
                                dynamicObject3["QuoteResult"] = "9";
                                if (this.QuoteSumDatas.ContainsKey(quoteData.QuoteBillId))
                                {
                                    QuoteSumData quoteSumData = this.QuoteSumDatas[quoteData.QuoteBillId];
                                    if (quoteSumData.IsMinQuote)
                                    {
                                        dynamicObject3["QuoteResult"] = "1";
                                    }
                                }
                            }
                            dynamicObject3["QuotePhone"] = quoteData.Phone;
                            dynamicObject3["QuoteAmount"] = quoteData.Amount;
                            dynamicObject3["QuoteAllAmount"] = quoteData.AllAmount;
                            foreach (FieldMapInfo fieldMapInfo in this.QuoteToCompareFieldMaps)
                            {
                                if (fieldMapInfo.TargetField is BaseDataField)
                                {
                                    string value = Convert.ToString(quoteData.Data[fieldMapInfo.SourceField.FieldName]);
                                    dynamicObject3[fieldMapInfo.TargetField.PropertyName + "_Id"] = value;
                                    FieldUtils.SetBaseDataFieldValue(base.Context, fieldMapInfo.TargetField as BaseDataField, dynamicObject3, value);
                                }
                                else
                                {
                                    dynamicObject3[fieldMapInfo.TargetField.PropertyName] = quoteData.Data[fieldMapInfo.SourceField.FieldName];
                                }
                            }
                            dynamicObjectCollection.Add(dynamicObject3);
                        }
                        string bjsql = $@"                     
	                    select * from T_SVM_Quote a
	                    inner join T_SVM_QuoteEntry b on a.fid=b.fid
	                    where
	                    a.FSRCINQUIRYBILLNO in
                        (select FBILLNO from T_SVM_INQUIRY where F_260_CSXJD IN 
                         (select F_260_CSXJD from T_SVM_INQUIRY where   fid='{dynamicObject2["SBillId"]}') and fid!='{dynamicObject2["SBillId"]}'
                          union all 
                          select FBILLNO from T_SVM_INQUIRY where fbillno IN 
                         (select F_260_CSXJD from T_SVM_INQUIRY where fid='{dynamicObject2["SBillId"]}') and fid!='{dynamicObject2["SBillId"]}')
						  and b.FMATERIALID in(SELECT FMATERIALID FROM T_SVM_INQUIRYENTRY where FENTRYID = '{dynamicObject2["SId"]}')";
                        var bjs = DBUtils.ExecuteDynamicObject(Context, bjsql);
                        foreach (var bj in bjs)
                        {
                            DynamicObject dynamicObject3 = new DynamicObject(entity.DynamicObjectType);
                            dynamicObject3["Seq"] = num++;
                            dynamicObject3["QuoteBillNo"] = bj["FBILLNO"];
                            dynamicObject3["QuoteDate"] = bj["FDATE"];
                            dynamicObject3["QuoteQty"] = bj["FQTY"];
                            dynamicObject3["QuoteBaseQty"] = bj["FBASEQTY"];
                            dynamicObject3["QuotePrice"] = bj["FPRICE"];
                            dynamicObject3["QuoteTaxPrice"] = bj["FTAXPRICE"];
                            dynamicObject3["TaxRate"] = bj["FTAXRATE"];
                            dynamicObject3["QuoteComfirmQty"] = bj["FCONFIRMQTY"];
                            dynamicObject3["QuoteComfirmBaseQty"] = bj["FBASECONFIRMQTY"];
                            dynamicObject3["QuoteComfirmPrice"] = bj["FCONFIRMPRICE"];
                            dynamicObject3["QuoteComfirmTaxPrice"] = bj["FCONFIRMTAXPRICE"];
                            dynamicObject3["QuoteBillEntryId"] = bj["FENTRYID"];
                            dynamicObject3["QuoteSupplier_id"] = bj["FSUPPLIERID"];
                            FieldUtils.SetBaseDataFieldValue(base.Context, field3, dynamicObject3, bj["FSUPPLIERID"]);
                            dynamicObject3["QuoteContacter"] = bj["FCONTACT"];
                            dynamicObject3["QuoteUnitID_id"] = bj["FUNITID"];
                            FieldUtils.SetBaseDataFieldValue(base.Context, field, dynamicObject3, bj["FUNITID"]);
                            dynamicObject3["QuoteBaseUnitId_id"] = bj["FBASEUNITID"];
                            FieldUtils.SetBaseDataFieldValue(base.Context, field2, dynamicObject3, bj["FBASEUNITID"]);
                            dynamicObject3["QuoteCurrencyId_id"] = bj["FREFERCURRID"];
                            FieldUtils.SetBaseDataFieldValue(base.Context, field4, dynamicObject3, bj["FREFERCURRID"]);
                            if (!flag)
                            {
                                if (flag2)
                                {
                                    dynamicObject3["QuoteResult"] = "9";
                                    flag2 = false;
                                }
                                else
                                {
                                    dynamicObject3["QuoteResult"] = "9";
                                }
                            }
                            else
                            {
                                dynamicObject3["QuoteResult"] = "9";
                                if (this.QuoteSumDatas.ContainsKey(Convert.ToInt64(bj["FID"])))
                                {
                                    QuoteSumData quoteSumData = this.QuoteSumDatas[Convert.ToInt64(bj["FID"])];
                                    if (quoteSumData.IsMinQuote)
                                    {
                                        dynamicObject3["QuoteResult"] = "9";
                                    }
                                }
                            }
                            dynamicObject3["QuotePhone"] = bj["FPHONE"];
                            dynamicObject3["QuoteAmount"] = bj["FAMOUNT"];
                            dynamicObject3["QuoteAllAmount"] = bj["FALLAMOUNT"];
                            dynamicObject3["F_260_XJC"] = bj["F_260_XJC"];
                            foreach (FieldMapInfo fieldMapInfo in this.QuoteToCompareFieldMaps)
                            {
                                if (fieldMapInfo.TargetField is BaseDataField)
                                {
                                    string value = Convert.ToString(bj["F_260_XJC"]);
                                    dynamicObject3[fieldMapInfo.TargetField.PropertyName + "_Id"] = value;
                                    FieldUtils.SetBaseDataFieldValue(base.Context, fieldMapInfo.TargetField as BaseDataField, dynamicObject3, value);
                                }
                                else
                                {
                                   dynamicObject3[fieldMapInfo.TargetField.PropertyName] = bj["F_260_XJC"];
                                }
                            }
                            dynamicObjectCollection.Add(dynamicObject3);

                        }
                    }
                }
            }
        }

        // Token: 0x0600006D RID: 109 RVA: 0x00008EFC File Offset: 0x000070FC
        public virtual void GetQuoteDataSourceData(DynamicObjectCollection SourceData)
        {
            if (SourceData != null && SourceData.Count > 0)
            {
                List<long> list = new List<long>();
                foreach (DynamicObject dynamicObject in SourceData)
                {

                    list.Add(Convert.ToInt64(dynamicObject["FInquiryEntry_FEntryID"]));
                }

                ITimeService timeService = ServiceFactory.GetTimeService(base.Context);
                DateTime systemDateTime = timeService.GetSystemDateTime(base.Context);
                if (list.Count > 0)
                {
                    this.SetQuoteBillToComparePriceFieldMaps();
                    string appendSelectFieldSQL = this.GetAppendSelectFieldSQL();
                    string strSQL = string.Format("SELECT {1} TH.FBILLNO,TH.FDATE,TH.FSUPPLIERID,TH.FContact,TH.FPHONE,\r\n                                        TR.FQTY,TR.FBASEQTY,TR.FUNITID,TR.FBASEUNITID,TR.FPRICE,TR.FTAXPRICE,TR.FTAXRATE,\r\n                                        TB.FSETTLECURRID,TLK.FSID as finquiryentryid,TR.FENTRYID as FQuoteBillEntryId,\r\n\t\t\t\t\t\t\t\t\t\tTR.FAMOUNT,TR.FALLAMOUNT,TH.FID as FQuoteBillId\r\n                                        FROM T_SVM_QUOTEENTRY_LK TLK\r\n                                        inner join T_SVM_QUOTEENTRY tr on tr.fentryid=tlk.fentryid AND tr.FQTY>0 AND tr.FPRICE>0  \r\n                                        inner join T_SVM_QUOTE tH on tr.FID=TH.FID\r\n                                        inner join T_SVM_QUOTEBUS tB on tB.FID=TH.FID\r\n                                        inner join (select /*+ cardinality(b {0})*/ fid from TABLE(fn_StrSplit(@FID, ',', 1)) b) T2 on TLK.FSID=T2.fid  \r\n                                        where TH.FDOCUMENTSTATUS='C' AND TH.FCANCELSTATUS='A' AND tH.FENDDATE>@CurrentDateTime", list.Distinct<long>().Count<long>(), appendSelectFieldSQL);
                    List<SqlParam> list2 = new List<SqlParam>();
                    list2.Add(new SqlParam("@FID", KDDbType.udt_inttable, list.Distinct<long>().ToArray<long>()));
                    list2.Add(new SqlParam("@CurrentDateTime", KDDbType.DateTime, systemDateTime));
                    DynamicObjectCollection dynamicObjectCollection = DBUtils.ExecuteDynamicObject(base.Context, strSQL, null, null, CommandType.Text, list2.ToArray());
                    if (dynamicObjectCollection != null && dynamicObjectCollection.Count > 0)
                    {
                        foreach (DynamicObject dynamicObject2 in dynamicObjectCollection)
                        {
                            QuoteData quoteData = new QuoteData();
                            quoteData.InquiryBillEntryId = Convert.ToInt64(dynamicObject2["finquiryentryid"]);
                            quoteData.QuoteBillEntryId = Convert.ToInt64(dynamicObject2["FQuoteBillEntryId"]);
                            quoteData.BillNO = Convert.ToString(dynamicObject2["FBILLNO"]);
                            quoteData.Date = Convert.ToDateTime(dynamicObject2["FDATE"]);
                            quoteData.Qty = Convert.ToDecimal(dynamicObject2["FQTY"]);
                            quoteData.BaseQty = Convert.ToDecimal(dynamicObject2["FBASEQTY"]);
                            quoteData.Price = Convert.ToDecimal(dynamicObject2["FPRICE"]);
                            quoteData.TaxPrice = Convert.ToDecimal(dynamicObject2["FTAXPRICE"]);
                            quoteData.TaxRate = Convert.ToDecimal(dynamicObject2["FTaxRate"]);
                            quoteData.UnitId = Convert.ToInt64(dynamicObject2["FUNITID"]);
                            quoteData.BaseUnitId = Convert.ToInt64(dynamicObject2["FBASEUNITID"]);
                            quoteData.SupplierId = Convert.ToInt64(dynamicObject2["FSUPPLIERID"]);
                            quoteData.Contact = Convert.ToString(dynamicObject2["FContact"]);
                            quoteData.CurrencyId = Convert.ToInt64(dynamicObject2["FSETTLECURRID"]);
                            quoteData.Phone = Convert.ToString(dynamicObject2["FPHONE"]);
                            quoteData.Amount = Convert.ToDecimal(dynamicObject2["FAMOUNT"]);
                            quoteData.AllAmount = Convert.ToDecimal(dynamicObject2["FALLAMOUNT"]);
                            quoteData.QuoteBillId = Convert.ToInt64(dynamicObject2["FQuoteBillId"]);
                            quoteData.Data = dynamicObject2;
                            this.AddQuoteDataItem(quoteData);
                            this.AddQuoteSumDataItem(quoteData);
                        }
                    }
                    if (this.QuoteSumDatas != null && this.QuoteSumDatas.Count > 0)
                    {
                        decimal minAllAmount = this.QuoteSumDatas.Values.Min((QuoteSumData q) => q.BillAllAmount);
                        (from p in this.QuoteSumDatas.Values
                         orderby p.QuoteBillNO
                         select p).FirstOrDefault((QuoteSumData p) => p.BillAllAmount == minAllAmount).SumComRst = "1";
                    }
                }
            }
        }

        // Token: 0x0600006E RID: 110 RVA: 0x000092F4 File Offset: 0x000074F4
        public void AddQuoteDataItem(QuoteData item)
        {
            long inquiryBillEntryId = item.InquiryBillEntryId;
            if (this.QuoteDatas.ContainsKey(inquiryBillEntryId))
            {
                this.QuoteDatas[inquiryBillEntryId].Add(item);
                return;
            }
            List<QuoteData> list = new List<QuoteData>();
            list.Add(item);
            this.QuoteDatas.Add(inquiryBillEntryId, list);
        }

        // Token: 0x0600006F RID: 111 RVA: 0x00009344 File Offset: 0x00007544
        public void AddQuoteSumDataItem(QuoteData item)
        {
            long quoteBillId = item.QuoteBillId;
            if (this.QuoteSumDatas.ContainsKey(quoteBillId))
            {
                QuoteSumData quoteSumData = this.QuoteSumDatas[quoteBillId];
                quoteSumData.BillAmount += item.Amount;
                quoteSumData.BillAllAmount += item.AllAmount;
                return;
            }
            QuoteSumData quoteSumData2 = new QuoteSumData();
            quoteSumData2.QuoteBillNO = item.BillNO;
            quoteSumData2.QuoteBillId = item.QuoteBillId;
            quoteSumData2.SupplierId = item.SupplierId;
            quoteSumData2.CurrencyId = item.CurrencyId;
            quoteSumData2.BillAmount = item.Amount;
            quoteSumData2.BillAllAmount = item.AllAmount;
            this.QuoteSumDatas.Add(quoteBillId, quoteSumData2);
        }

        // Token: 0x06000070 RID: 112 RVA: 0x00009420 File Offset: 0x00007620
        public void SetQuoteBillToComparePriceFieldMaps()
        {
            ConvertRuleMetaData convertRule = ServiceHelper.GetService<IConvertService>().GetConvertRule(base.Context, "SVM_QuoteBill-SVM_ComparePrice_FC");
            if (convertRule != null)
            {
                ConvertRuleElement rule = convertRule.Rule;
                if (rule != null)
                {
                    DefaultConvertPolicyElement defaultConvertPolicyElement = (DefaultConvertPolicyElement)rule.Policies[2];
                    if (defaultConvertPolicyElement != null)
                    {
                        IMetaDataService service = ServiceHelper.GetService<IMetaDataService>();
                        FormMetadata formMetadata = service.Load(base.Context, "SVM_ComparePrice", true) as FormMetadata;
                        List<Field> fieldList = formMetadata.BusinessInfo.GetFieldList();
                        using (IEnumerator<FieldMapElement> enumerator = defaultConvertPolicyElement.FieldMaps.GetEnumerator())
                        {
                            while (enumerator.MoveNext())
                            {
                                FieldMapElement item = enumerator.Current;
                                if (item.SourceFieldKey != null && item.SourceFieldKey.Length > 0)
                                {
                                    Field targetField = fieldList.FirstOrDefault((Field p) => p.Key == item.TargetFieldKey);
                                    this.QuoteToCompareFieldMaps.Add(new FieldMapInfo
                                    {
                                        TargetFieldKey = item.TargetFieldKey,
                                        SourceFieldKey = item.SourceFieldKey,
                                        TargetField = targetField
                                    });
                                }
                            }
                        }
                    }
                }
            }
        }

        // Token: 0x06000071 RID: 113 RVA: 0x00009594 File Offset: 0x00007794
        public virtual string GetAppendSelectFieldSQL()
        {
            string text = string.Empty;
            if (this.QuoteToCompareFieldMaps.Count > 0)
            {
                IMetaDataService service = ServiceHelper.GetService<IMetaDataService>();
                FormMetadata formMetadata = service.Load(base.Context, "SVM_QuoteBill", true) as FormMetadata;
                List<Field> fieldList = formMetadata.BusinessInfo.GetFieldList();
                using (List<FieldMapInfo>.Enumerator enumerator = this.QuoteToCompareFieldMaps.GetEnumerator())
                {
                    while (enumerator.MoveNext())
                    {
                        FieldMapInfo item = enumerator.Current;
                        Field field = fieldList.FirstOrDefault((Field p) => p.Key == item.SourceFieldKey);
                        if (field != null)
                        {
                            item.SourceField = field;
                            if (field.Entity is HeadEntity)
                            {
                                text = text + "TH." + field.FieldName + ",";
                            }
                            else if (string.Compare(field.TableName, "T_SVM_QUOTEBUS", true) == 0)
                            {
                                text = text + "TB." + field.FieldName + ",";
                            }
                            else if (string.Compare(field.TableName, "T_SVM_QUOTEENTRY", true) == 0)
                            {
                                text = text + "TR." + field.FieldName + ",";
                            }
                        }
                    }
                }
            }
            return text;
        }

        // Token: 0x04000023 RID: 35
        private const string QuoteResult_UpdateInquiry = "1";

        // Token: 0x04000024 RID: 36
        private const string QuoteResult_Ignore = "9";

        // Token: 0x04000025 RID: 37
        private Dictionary<long, List<QuoteData>> QuoteDatas = new Dictionary<long, List<QuoteData>>();

        // Token: 0x04000026 RID: 38
        private Dictionary<long, QuoteSumData> QuoteSumDatas = new Dictionary<long, QuoteSumData>();

        // Token: 0x04000027 RID: 39
        private BusinessInfo TargetBusinessInfo;

        // Token: 0x04000028 RID: 40
        public List<FieldMapInfo> QuoteToCompareFieldMaps = new List<FieldMapInfo>();
    }
}
