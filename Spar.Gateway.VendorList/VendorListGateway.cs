using System;
using System.Data.Linq;
using System.Linq;
using System.Transactions;
using BizToDBIntegration.WcfContracts;
using Spar.Data.XsdModels.Vendors;
using System.Collections.Generic;

namespace Spar.Gateway.VendorList
{

    class VendorListGateway
    {
        public Response Save(string xmlData, Response returnResponse)
        {
            var vendorListReceived = SPARVendors.Parse(xmlData);

            returnResponse = ProcessWarehouseVendors(vendorListReceived, returnResponse);
            returnResponse = ProcessDropshipmentVendors(vendorListReceived, returnResponse);

            return returnResponse;
        }

        private Response ProcessDropshipmentVendors(SPARVendors vendorListReceived, Response returnResponse)
        {
            int dropshipVendorsUpdated = 0;
            int dropshipVendorsAdded = 0;

            using (var ts = new TransactionScope(TransactionScopeOption.Required,
                    new TransactionOptions { IsolationLevel = IsolationLevel.ReadUncommitted }))
            {
                try
                {
                    //VendorAccType
                    var linqXmlVendors = (from vendor in vendorListReceived.SPARVendor
                                          where vendor.VendorAccType == "BOTH" || vendor.VendorAccType == "DSH"
                                          select vendor).ToList();

                    if (!linqXmlVendors.Any())
                    {
                        returnResponse.Trace.Add("No Dropshipment Vendors");

                        return returnResponse;
                    }

                    var buEanCode = linqXmlVendors.FirstOrDefault().BuEanCode;

                    using (var db = new VendorDataContext())
                    {
                        var correctDcEan = (from dc in db.DCs
                                            where dc.DCcX40EAN == buEanCode || dc.DCcEANNumber == buEanCode
                                            select dc.DCcEANNumber).FirstOrDefault();

                        if (correctDcEan == null)
                        {
                            returnResponse.ResponseResult = ResponseResult.Exception;
                            returnResponse.Trace.Add(String.Format("BuEanCode {0} could not be mapped to a valid Dc - Dropshipment Vendors",
                                                                   buEanCode));
                            returnResponse.ResponseMessage = "Transaction failed saving to DB";

                            return returnResponse;
                        }

                        var suppliers = (from supplier in db.Suppliers
                                         select supplier).ToList();

                        var vendors = (from vendor in db.SupplierDCLookups
                                       where vendor.BuEanCode == correctDcEan
                                       select vendor).ToList();

                        // Update Vendors
                        foreach (var vendor in vendors)
                        {
                            var uv = linqXmlVendors
                                .FirstOrDefault(x => x.VendorCode == vendor.VendorCode);

                            if (uv == null) continue;

                            vendor.BuEanCode = correctDcEan;
                            vendor.VendorCode = uv.VendorCode;
                            vendor.VendorStatus = uv.VendorStatus.ToCharArray()[0];
                            vendor.VendorName = uv.VendorName.Replace("&amp;", "&").Replace("apos", "");
                            vendor.Address1 = uv.Address1;
                            vendor.Address2 = uv.Address2;
                            vendor.Address3 = uv.Address3;
                            vendor.PostalCode = uv.PostalCode;
                            vendor.PhoneNumber = uv.PhoneNumber;
                            vendor.CountryCode = uv.CountryCode;
                            vendor.LocationCode = uv.LocationCode;
                            vendor.DespatchPoint = uv.DespatchPoint.Length != 13 ? String.Empty : uv.DespatchPoint;
                            vendor.EmailAddress = uv.EmailAddress;
                            vendor.SupplierVatNumber = uv.SupplierVatNumber;
                            vendor.CaptureClaim = uv.CaptureClaim.ToCharArray()[0];
                            vendor.CannotMatch = suppliers.Any(s => s.SPcEANNumber == vendor.LocationCode.Trim()
                                                                    || s.SPcEANNumber == vendor.DespatchPoint.Trim())
                                                     ? 0
                                                     : 1;
                            vendor.VendorAccountType = uv.VendorAccType;
                            vendor.StoreOrderMethod = uv.StoreOrderMethod;
                            vendor.EdiGroupCode = uv.EDIGroupCode;
                            vendor.EdiGroupCodeEan = uv.EDIGroupEan;

                            dropshipVendorsUpdated++;
                        }



                        // Add new Vendors
                        var activeNames = new HashSet<string>(vendors.Select(x => x.VendorCode));
                        var vendorsToAdd = linqXmlVendors.Where(x => !activeNames.Contains(x.VendorCode))
                                                         .ToList();

                        foreach (var v in vendorsToAdd)
                        {
                            var newVendor = new SupplierDCLookup
                            {
                                BuEanCode = correctDcEan,
                                VendorCode = v.VendorCode,
                                VendorStatus = v.VendorStatus.ToCharArray()[0],
                                VendorName = v.VendorName.Replace("&amp;", "&").Replace("apos", ""),
                                Address1 = v.Address1,
                                Address2 = v.Address2,
                                Address3 = v.Address3,
                                PostalCode = v.PostalCode,
                                PhoneNumber = v.PhoneNumber,
                                CountryCode = v.CountryCode,
                                LocationCode = v.LocationCode,
                                DespatchPoint = v.DespatchPoint,
                                EmailAddress = v.EmailAddress,
                                SupplierVatNumber = v.SupplierVatNumber,
                                CaptureClaim = v.CaptureClaim.ToCharArray()[0],
                                UpdateDate = DateTime.Now,
                                CannotMatch = suppliers.Any(s => s.SPcEANNumber == v.LocationCode.Trim()
                                                                 || s.SPcEANNumber == v.DespatchPoint.Trim())
                                                  ? 0
                                                  : 1,
                                VendorAccountType = v.VendorAccType,
                                StoreOrderMethod = v.StoreOrderMethod,
                                EdiGroupCode = v.EDIGroupCode,
                                EdiGroupCodeEan = v.EDIGroupEan
                            };


                            db.SupplierDCLookups.InsertOnSubmit(newVendor);

                            dropshipVendorsAdded++;
                        }

                        try
                        {
                            db.SubmitChanges(ConflictMode.ContinueOnConflict);
                        }
                        catch (ChangeConflictException e)
                        {
                            foreach (var occ in db.ChangeConflicts)
                            {
                                Console.WriteLine(e.Message);

                                occ.Resolve(RefreshMode.KeepChanges);
                            }
                        }
                    }
                    ts.Complete();

                    returnResponse.ResponseResult = ResponseResult.Success;
                    returnResponse.ResponseMessage = "Transaction saved to DB";
                    returnResponse.Trace.Add(
                        String.Format("{0} new Dropshipment Vendors(s) added and {1} Dropshipment Vendors(s) updated",
                                      dropshipVendorsAdded, dropshipVendorsUpdated));

                    return returnResponse;
                }
                catch (Exception e)
                {
                    returnResponse.ResponseResult = ResponseResult.Exception;
                    returnResponse.ResponseMessage = "Transaction failed saving to DB";
                    returnResponse.Trace.Add(e.Message);

                    return returnResponse;
                }
            }
        }


        private Response ProcessWarehouseVendors(SPARVendors vendorListReceived, Response returnResponse)
        {
            int warehouseVendorsUpdated = 0;
            int warehouseVendorsAdded = 0;

            using (var ts = new TransactionScope(TransactionScopeOption.Required,
                    new TransactionOptions { IsolationLevel = IsolationLevel.ReadUncommitted }))
            {
                try
                {
                    //VendorAccType
                    var linqXmlVendors = (from vendor in vendorListReceived.SPARVendor
                                          where vendor.VendorAccType == "BOTH" || vendor.VendorAccType == "WHSE"
                                          select vendor).ToList();

                    if (!linqXmlVendors.Any())
                    {
                        returnResponse.Trace.Add("No Warehouse Vendors");

                        return returnResponse;
                    }

                    var buEanCode = linqXmlVendors.FirstOrDefault().BuEanCode;

                    using (var db = new WarehouseVendorsDataContext())
                    {
                        var correctDcEan = (from company in db.Companies
                                            where company.Ean == buEanCode || company.X400 == buEanCode
                                            select company.Ean).FirstOrDefault();

                        if (correctDcEan == null)
                        {
                            returnResponse.ResponseResult = ResponseResult.Exception;
                            returnResponse.Trace.Add(String.Format("BuEanCode {0} could not be mapped to a valid Dc - Warehouse Vendors", buEanCode));
                            returnResponse.ResponseMessage = "Transaction failed saving to DB";

                            return returnResponse;
                        }

                        //var suppliers = (from supplier in db.SupplierWarehouses
                        //                 select supplier).ToList();

                        var vendors = (from vendor in db.SupplierDCLookupWarehouses
                                       where vendor.BuEanCode == correctDcEan
                                       select vendor).ToList();

                        // Update Vendors
                        foreach (var vendor in vendors)
                        {


                            var uv = linqXmlVendors
                                .FirstOrDefault(x => x.VendorCode == vendor.VendorCode);

                            if (uv == null) continue;

                            vendor.BuEanCode = correctDcEan;
                            vendor.VendorCode = uv.VendorCode;
                            vendor.VendorStatus = uv.VendorStatus.ToCharArray()[0];
                            vendor.VendorName = uv.VendorName.Replace("&amp;", "&").Replace("apos", "");
                            vendor.Address1 = uv.Address1;
                            vendor.Address2 = uv.Address2;
                            vendor.Address3 = uv.Address3;
                            vendor.PostalCode = uv.PostalCode;
                            vendor.PhoneNumber = uv.PhoneNumber;
                            vendor.CountryCode = uv.CountryCode;
                            vendor.LocationCode = uv.LocationCode;
                            vendor.DespatchPoint = uv.DespatchPoint;
                            vendor.EmailAddress = uv.EmailAddress;
                            vendor.SupplierVatNumber = uv.SupplierVatNumber;
                            vendor.CaptureClaim = uv.CaptureClaim.ToCharArray()[0];
                            vendor.CannotMatch = 0;
                            vendor.VendorAccountType = uv.VendorAccType;
                            //vendor.CannotMatch = suppliers.Any(s => s. == vendor.LocationCode.Trim()
                            //    || s.SPcEANNumber == vendor.DespatchPoint.Trim()) ? 0 : 1;

                            vendor.StoreOrderMethod = uv.StoreOrderMethod;
                            vendor.EdiGroupCode = uv.EDIGroupCode;
                            vendor.EdiGroupCodeEan = uv.EDIGroupEan;


                            warehouseVendorsUpdated++;
                        }



                        // Add new Vendors
                        var activeNames = new HashSet<string>(vendors.Select(x => x.VendorCode));
                        var vendorsToAdd = linqXmlVendors.Where(x => !activeNames.Contains(x.VendorCode))
                                                        .ToList();

                        foreach (var v in vendorsToAdd)
                        {


                            var newVendor = new SupplierDCLookupWarehouse()
                            {
                                BuEanCode = correctDcEan,
                                VendorCode = v.VendorCode,
                                VendorStatus = v.VendorStatus.ToCharArray()[0],
                                VendorName = v.VendorName.Replace("&amp;", "&").Replace("apos", ""),
                                Address1 = v.Address1,
                                Address2 = v.Address2,
                                Address3 = v.Address3,
                                PostalCode = v.PostalCode,
                                PhoneNumber = v.PhoneNumber,
                                CountryCode = v.CountryCode,
                                LocationCode = v.LocationCode,
                                DespatchPoint = v.DespatchPoint,
                                EmailAddress = v.EmailAddress,
                                SupplierVatNumber = v.SupplierVatNumber,
                                CaptureClaim = v.CaptureClaim.ToCharArray()[0],
                                UpdateDate = DateTime.Now,
                                //    CannotMatch = suppliers.Any(s => s.SPcEANNumber == v.LocationCode.Trim()
                                //|| s.SPcEANNumber == v.DespatchPoint.Trim()) ? 0 : 1,
                                VendorAccountType = v.VendorAccType,
                                StoreOrderMethod = v.StoreOrderMethod,
                                EdiGroupCode = v.EDIGroupCode,
                                EdiGroupCodeEan = v.EDIGroupEan
                            };



                            db.SupplierDCLookupWarehouses.InsertOnSubmit(newVendor);

                            warehouseVendorsAdded++;

                        }

                        try
                        {
                            db.SubmitChanges(ConflictMode.ContinueOnConflict);
                        }
                        catch (ChangeConflictException e)
                        {
                            foreach (var occ in db.ChangeConflicts)
                            {
                                Console.WriteLine(e.Message);

                                occ.Resolve(RefreshMode.KeepChanges);
                            }
                        }
                    }
                    ts.Complete();

                    returnResponse.ResponseResult = ResponseResult.Success;
                    returnResponse.ResponseMessage = "Transaction saved to DB";
                    returnResponse.Trace.Add(String.Format("{0} new Warehouse Vendors(s) added and {1} Warehouse Vendors(s) updated",
                        warehouseVendorsAdded, warehouseVendorsUpdated));

                    return returnResponse;
                }
                catch (Exception e)
                {
                    Console.WriteLine("ERROR");
                    returnResponse.ResponseResult = ResponseResult.Exception;
                    returnResponse.ResponseMessage = "Transaction failed saving to DB";
                    returnResponse.Trace.Add(e.Message);

                    return returnResponse;
                }
            }

        }
    }
}
