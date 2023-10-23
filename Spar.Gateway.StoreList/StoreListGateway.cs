using BizToDBIntegration.WcfContracts;
using System;
using System.Collections.Generic;
using System.Data.Linq;
using System.Linq;
using System.Transactions;

namespace Spar.Gateway.StoreList
{
    internal sealed class StoreListGateway
    {
        private enum Action : int
        {
            New,
            Updated,
            Deleted
        };

        public Response Save(string xmlData, Response returnResponse)
        {
            try
            {
                var linqXml = Spar.Data.XsdModels.StoreList.Stores.StoreList.Parse(xmlData);
                var linqStores = (from store in linqXml.Store
                                  select store);
                // These store lists are per DC, all distribution ids will be the same
                string distributionId = linqStores.FirstOrDefault().DistributionId.ToString();

                int dcId = 0;
                using (StoreListDataContext db = new StoreListDataContext())
                {
                    dcId = (from dcList in db.Lookup_DCIds
                            where dcList.DistributionId == distributionId
                            select dcList.DCId).SingleOrDefault();
                    if (dcId == 0)
                    {
                        returnResponse.ResponseResult = ResponseResult.Exception;
                        returnResponse.ResponseMessage = "StoreList failure";
                        returnResponse.Trace.Add("StoreList DistributionId does not exist in DB");
                        return returnResponse;
                    }
                }

                returnResponse = InsertStoreList(linqStores, dcId, returnResponse);
                returnResponse.ResponseResult = ResponseResult.Success;
                returnResponse.ResponseMessage = "Transaction saved to DB";
                return returnResponse;
            }
            catch (Exception ex)
            {
                returnResponse.ResponseResult = ResponseResult.Exception;
                returnResponse.ResponseMessage = "Transaction failed writing to DB";
                returnResponse.Trace.Add(ex.Message);
                return returnResponse;
            }
        }

        private Response InsertStoreList(IEnumerable<Data.XsdModels.StoreList.Stores.StoreList.StoreLocalType> linqStores, int dcId, Response returnResponse)
        {
            int counterAdd = 0;
            int counterUpdate = 0;

            using (var ts = new TransactionScope(TransactionScopeOption.Required,
                new TransactionOptions() { IsolationLevel = IsolationLevel.ReadUncommitted }))
            {
                try
                {
                    using (StoreListDataContext db = new StoreListDataContext())
                    {
                        foreach (var linqStore in linqStores)
                        {
                            // Each store will have only one Gln code
                            var query = (from s in db.Stores
                                         where s.Gln == linqStore.Gln && s.DcId == dcId
                                            && s.Code == linqStore.Code
                                         select s).FirstOrDefault();

                            if (query != null)
                            {
                                // Update store
                                query.Code = linqStore.Code;
                                query.Gln = linqStore.Gln;
                                query.Name = linqStore.Name;
                                query.OwnerName = linqStore.OwnerName;
                                query.ManagerName = linqStore.ManagerName;
                                query.Address = String.Concat(linqStore.Address.AddressLine1, ' ',
                                                              linqStore.Address.AddressLine2, ' ',
                                                              linqStore.Address.AddressLine3, ' ',
                                                              linqStore.Address.AddressLine4);
                                query.PhoneNumber = linqStore.PhoneNumber;
                                query.FaxNumber = linqStore.FaxNumber;
                                query.Email = linqStore.Email;
                                if (linqStore.Status == "C")
                                {
                                    query.IsLive = 0;
                                }
                                else
                                {
                                    query.IsLive = 1;
                                }
                                
                                query.Vat = linqStore.Vat;
                                query.FormatType = linqStore.FormatType;
                                query.CountryCode = linqStore.CountryCode;
                                query.ClaimsForSupplierIndicator = linqStore.ClaimsForSupplierIndicator;
                                query.ClaimCaptureOverrideIndicator =
                                    String.IsNullOrEmpty(query.ClaimCaptureOverrideIndicator) ?
                                    linqStore.ClaimsForSupplierIndicator :
                                    query.ClaimCaptureOverrideIndicator;
                                query.ActionDate = DateTime.Now;
                                query.Action = (int)Action.Updated;
                                query.DcId = dcId;
                                query.STcExportCustInd = linqStore.ExportCustIndicator;
                                query.STcItemSocRqd = linqStore.FullItemSocRequired;
                                query.STcExportVatInd = linqStore.VATIndicator;

                                counterUpdate++;
                                //db.SubmitChanges();


                            }
                            else
                            {
                                // Add new store
                                User user = (from u in db.Users
                                             where u.Name == linqStore.Gln
                                             select u).FirstOrDefault();

                                Store store = new Store()
                                {
                                    Code = linqStore.Code,
                                    Gln = linqStore.Gln,
                                    Name = linqStore.Name,
                                    OwnerName = linqStore.OwnerName,
                                    ManagerName = linqStore.ManagerName,
                                    Address = String.Concat(linqStore.Address.AddressLine1, ' ',
                                                                linqStore.Address.AddressLine2, ' ',
                                                                linqStore.Address.AddressLine3, ' ',
                                                                linqStore.Address.AddressLine4),
                                    PhoneNumber = linqStore.PhoneNumber,
                                    FaxNumber = linqStore.FaxNumber,
                                    Email = linqStore.Email,
                                    IsLive = 2,
                                    Vat = linqStore.Vat,
                                    FormatType = linqStore.FormatType,
                                    CountryCode = linqStore.CountryCode,
                                    ClaimsForSupplierIndicator = linqStore.ClaimsForSupplierIndicator,
                                    ClaimCaptureOverrideIndicator = linqStore.ClaimsForSupplierIndicator,
                                    ActionDate = DateTime.Now,
                                    DcId = dcId,
                                    ParentId = 0,
                                    CreatedDate = DateTime.Now,
                                    Action = (int)Action.New,
                                    User = user ?? new User
                                    {
                                        Name = linqStore.Gln,
                                        Password = "password",
                                        Type = 3,
                                        Permission = 0,
                                        ChangePassword = 1,
                                        Disabled = 0,
                                        ParentId = 0,
                                        DcId = dcId
                                    },
                                    STcExportCustInd = linqStore.ExportCustIndicator,
                                    STcItemSocRqd = linqStore.FullItemSocRequired,
                                    STcExportVatInd = linqStore.VATIndicator
                                };

                                store.StoreEmails.Add(
                                    new StoreEmail()
                                    {
                                        Email = linqStore.Email
                                    });

                                counterAdd++;


                                db.Stores.InsertOnSubmit(store);

                            }
                        } // foreach


                        try
                        {
                            db.SubmitChanges(ConflictMode.ContinueOnConflict);
                        }
                        catch (ChangeConflictException e)
                        {
                            foreach (ObjectChangeConflict occ in db.ChangeConflicts)
                            {
                                //No database values are merged into current.
                                occ.Resolve(RefreshMode.OverwriteCurrentValues);
                            }

                        }
                    } // db

                    ts.Complete();
                    returnResponse.ResponseResult = ResponseResult.Success;
                    returnResponse.ResponseMessage = "Store list saved to DB";
                    returnResponse.Trace.Add(String.Format("{0} new store(s) added and {1} stores updated", counterAdd,
                             counterUpdate));
                }

                catch (Exception ex)
                {
                    returnResponse.ResponseResult = ResponseResult.Exception;
                    returnResponse.ResponseMessage = "Store list failed writing to DB";
                    returnResponse.Trace.Add(ex.Message);
                    return returnResponse;
                }

                if (linqStores.FirstOrDefault().DistributionId != "6001008999895")
                {
                    returnResponse = MarkStoreInactive(linqStores, dcId, returnResponse);
                }

                return returnResponse;
            } // transaction scope
        }

        private Response MarkStoreInactive(IEnumerable<Data.XsdModels.StoreList.Stores.StoreList.StoreLocalType> linqStores, int dcId, Response returnResponse)
        {
            int counter = 0;

            using (var ts = new TransactionScope(TransactionScopeOption.Required,
               new TransactionOptions() { IsolationLevel = IsolationLevel.ReadUncommitted }))
            {
                try
                {
                    using (StoreListDataContext db = new StoreListDataContext())
                    {
                        var markStoresInactive = (from s in db.Stores
                                                  where s.DcId == dcId
                                                  select s);

                        foreach (var store in markStoresInactive)
                        {
                            if ((from ls in linqStores
                                 where ls.Gln == store.Gln
                                 select ls).Count() == 0)
                            {
                                store.Action = (int)Action.Deleted;
                                store.IsLive = 0;
                                store.ActionDate = DateTime.Now;

                                counter++;
                            }
                        };
                        try
                        {
                            db.SubmitChanges(ConflictMode.ContinueOnConflict);
                        }
                        catch (ChangeConflictException e)
                        {
                            foreach (ObjectChangeConflict occ in db.ChangeConflicts)
                            {
                                //No database values are merged into current.
                                occ.Resolve(RefreshMode.KeepChanges);
                            }
                        }
                    }
                    ts.Complete();

                    returnResponse.ResponseMessage = "Transaction saved to DB";
                    returnResponse.Trace.Add(String.Format("{0} store(s) marked as inactive", counter));
                    return returnResponse;
                }
                catch (Exception ex)
                {
                    returnResponse.ResponseResult = ResponseResult.Exception;
                    returnResponse.Trace.Add(String.Format("{0} store(s) failed to be marked as inactive", counter));
                    return returnResponse;
                }
            } // tranasction scope
        }
    }
}

