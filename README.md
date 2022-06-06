# MyFeeder

Project home is at [sourceforge](https://sourceforge.net/projects/myfeeder/).

This is the source to Windows App Store App [MyFeeder](https://www.microsoft.com/store/apps/9WZDNCRDS451) with the following changes

- Support for Windows Mobile removed.
- ```<uap:Capability Name="sharedUserCertificates" />``` added to support standard readers.

The identity of the package is the same as used when submitting to the store.

```
% curl https://bspmts.mp.microsoft.com/v1/public/catalog/Retail/Products/9WZDNCRDS451/applockerdata
{
  "packageFamilyName": "9611rhubarb.geek.nz.MyFeeder_wyge0ckx2e380",
  "packageIdentityName": "9611rhubarb.geek.nz.MyFeeder",
  "windowsPhoneLegacyId": "60e43050-f899-45f3-9c28-b754e0ace8a9",
  "publisherCertificateName": "CN=4A20E80D-8F0C-4307-920F-3EDFD17061EA"
}
```
