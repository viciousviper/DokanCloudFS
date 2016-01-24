# DokanCloudFS
**DokanCloudFS** is a virtual filesystem for various publicly accessible cloud storage services on the Microsoft Windows platform.

| Branch  | Build status | Code coverage |
| :------ | :----------: | :-----------: |
| master  | [![Build status](https://ci.appveyor.com/api/projects/status/fynac58uetvtt43t/branch/master?svg=true)](https://ci.appveyor.com/project/viciousviper/dokancloudfs) | [![codecov.io](https://codecov.io/github/viciousviper/DokanCloudFS/coverage.svg?branch=master)](https://codecov.io/github/viciousviper/DokanCloudFS?branch=master)  |
| develop | [![Build status](https://ci.appveyor.com/api/projects/status/fynac58uetvtt43t/branch/develop?svg=true)](https://ci.appveyor.com/project/viciousviper/dokancloudfs) | [![codecov.io](https://codecov.io/github/viciousviper/DokanCloudFS/coverage.svg?branch=develop)](https://codecov.io/github/viciousviper/DokanCloudFS?branch=develop) |

## Objective

**DokanCloudFS** implements a virtual filesystem that allows direct mounting of various publicly accessible cloud storage services on the Microsoft Windows platform.

Mounted cloud storage drives appear as ordinary removable drives in Windows Explorer (albeit very slow ones) and can be used just about like any local drive or file share. All content written through DokanCloudFS is transparently encrypted on the cloud storage backend.

Some limitations apply concerning transfer speed, maximum file size, permissions, and alternate streams depending on parameters of the cloud storage service used as a backend.

## Features

- Mounting of cloud storage volumes as removable drives into Windows Explorer
  - number of mounted volumes only limited by available drive letters
  - mounting of multiple accounts of the same cloud storage service in parallel
- Full support for interactive file manipulation in Winows Explorer including but not limited to
  - copying and moving of multiple files and folders between DokanCloudFS drives and other drives
  - opening of files in default application via Mouse double click
  - right-click-menu on drive, files, and folders with access to menu commands and drive/file properties
  - thumbnails display for media files
- transparent encryption and decryption of all file content via AESCrypt
  - encryption key configurable per drive

## Supported Cloud storage services

DokanCloudFS requires a gateway assembly for any cloud storage service to be used as a backend.

The expected gateway interface types and a set of prefabricated gateways can be taken from the GitHub repository of the related [CloudFS](https://github.com/viciousviper/CloudFS) project.<br />The associated NuGet packages [CloudFS](https://www.nuget.org/packages/CloudFS/) and [CloudFS-Signed](https://www.nuget.org/packages/CloudFS-Signed/) include preconfigured API keys for the included cloud storage services and are ready to use. Unless marked otherwise, CloudFS NuGet packages should be used in a version matching the DokanCloudFS version.

## System Requirements

- Platform
  - .NET 4.6
- Drivers
  - [Dokany](https://github.com/dokan-dev/dokany/releases) driver 0.8.0 or greater
- Operating system
  - tested on Windows 8.1 x64 and Windows Server 2012 R2 (until version 1.0.0-alpha) /<br/>Windows 10 x64 (from version 1.0.1-alpha)
  - expected to run on Windows 7/8/8.1/10 and Windows Server 2008(R2)/2012(R2)

## Local compilation

- Compile the [CloudFS](https://github.com/viciousviper/CloudFS) solution
- Copy all assemblies from the *Gateways* directory in the build output of *CloudFS.GatewayTests* to the *Library* directory of DokanCloudFS
- Compile the DokanCloudFS solution
- Check that the *Gateways* directory in the build output of *DokanCloudFS.Mounter* contains all desired gateway assemblies and their dependencies (e.g. *Newtonsoft.JSON.dll*)

## Usage

- Configure the desired mount points in *DokanCloudFS.Mounter*'s configuration file *App.config*. See below for a sample configuration.
- Run *IgorSoft.DokanCloudFS.Mounter.exe* from the command line

### Sample configuration

```xml
  <mount libPath="..\..\..\Library" threads="5">
    <drives>
      <drive schema="onedrive" userName="OneDriveUser" root="Q:" encryptionKey="MyOneDriveSecret&amp;I" timeout="300" />
      <!--<drive schema="box" userName="BoxUser" root="R:" encryptionKey="MyBoxSecret&amp;I" timeout="300" />-->
      <!--<drive schema="copy" userName="CopyUser" root="S:" encryptionKey="MyCopySecret&amp;I" timeout="300" />-->
      <drive schema="file" root="T:" encryptionKey="MyFileSecret&amp;I" parameters="root=..\..\..\TestData" />
      <!--<drive schema="gdrive" userName="GDriveUser" root="U:" encryptionKey="MyGDriveSecret&amp;I" timeout="300" />-->
      <!--<drive schema="mega" userName="MegaUser" root="V:" encryptionKey="MyMegaSecret&amp;I" timeout="300" />-->
      <!--<drive schema="pcloud" userName="pCloudUser" root="W:" encryptionKey="MypCloudSecret&amp;I" timeout="300" />-->
    </drives>
  </mount>
```

Configuration options:

  - Global
    - **libPath**: Path to search for gateway plugin assemblies (relative to the location of *DokanCloudFS.exe*).<br/>All plugin dependencies not covered directly by DokanCloudFS should be placed in this path as well.
    - **threads**: Number of concurrent threads used by the Dokan driver.<br />Defaults to 5.
  - Per drive
    - **schema**: Selects the cloud storage service gateway to be used.<br />Must correspond to one of the *CloudFS* gateways installed in the *libPath* subdirectory. Presently supports the following values:
      - *onedrive* (tested)
      - *box*, *copy*, *gdrive*, *mega*, *pcloud* (test pending)
      - *file* (tested - mounting of local folders only)
    - **userName**: User account to be displayed in the mounted drive label.
    - **root**: The drive letter to be used as mount point for the cloud drive.<br />Choose a free drive letter such as *L:*.
    - **encryptionKey**: An arbitrary symmetric key for the transparent client-side AES encryption.<br />Leave this empty only if you *really* want to store content without encryption.
    - **parameters**: Custom parameters as required by the specific cloud storage service gateway. Multiple parameters are separated by a pipe-character `|`.
      - *file* gateway - requires a *root*-parameter specifying the target directory (e.g. `parameters="root=X:\Encrypted"`)
      - other gateways - no custom parameters supported so far
    - **timeout:** The timeout value for file operations on this drive measured in seconds.<br />A value of *300* should suffice for all but the slowest connections.

## Limitations

  - DokanCloudFS only supports mounting of a cloud storage volume's root directory.
  - The maximum supported file size varies between different cloud storage services. Moreover, the precise limits are not always disclosed.<br />Exceeding the size limit in a file writing operation will result in either a timout or a service error.
  - The files in a DokanCloudFS drive do not allow true random access, instead they are rather read or written as a whole.<br />Depending on the target application it *may* be possible to edit a file directly in the DokanCloudFS drive, otherwise it must be copied to a conventional drive for processing.
  - DokanCloudFS keeps an internal cache of directory metadata for increased performance. Changes made to the cloud storage volume outside of DokanCloudFS will not be automatically synchronized with this cache, therefore any form of concurrent write access may lead to unexpected results or errors.
  - The only encrypted file format supported by DokanCloudFS is the AESCrypt file format.
  - DokanCloudFS distinguishes encryption keys on a per-drive scale only. It is not possible to assign encryption keys to specific subdirectories or to individual files.<br />Although you could, in theory, mount several copies of the same cloud service volume in parallel, these copies will not synchronize their cached directory structures in any way (see above).<br />A future version of DokanCloudFS will support *read-only* access to unencrypted content on the cloud storage volume.

## Remarks

Due to the nature of the Dokan kernel-mode drivers involved, any severe errors inside DokanCloudFS can result in Windows Bluescreens.

**Don't install this project on production environments.**

You have been warned.

## Release Notes

| Date       | Version     | Comments                                                                       |
| :--------- | :---------- | :----------------------------------------------------------------------------- |
| 2016-01-24 | 1.0.2-alpha | - Gateway configuration extended to accept custom parameters. This change **breaks compatibility** with earlier API versions.<br/>- File Gateway configuration added in App.config |
| 2016-01-20 | 1.0.1-alpha | - NuGet dependencies updated, tests for CloudOperations made offline executable, code coverage analysis via codecov configured |
| 2016-01-10 | 1.0.0-alpha | - Initial release                                                              |
| 2015-12-30 | 1.0.0.0     | - Initial commit<br/>This version has not been extensively tested apart from the OneDrive gateway - **expect bugs**. |

## Future plans

- improve stability
- improve performance
- allow alternate encryption schemes
- allow read-access to unencrypted content on cloud storage volumes
- allow mounting and unmounting of individual drives without restarting the mounter process

## References

DokanCloudFS would not have been possible without the expertise and great effort devoted by their respective creators to the following projects:

- **Dokany** - The User mode file system library for windows (see [Dokan](http://dokan-dev.github.io) for the entire Dokany ecosystem, [Dokany](https://github.com/dokan-dev/dokany) for the specific driver)
- **Dokan.NET** - The Dokan DotNET wrapper (see [Dokan.NET Binding](https://github.com/dokan-dev/dokan-dotnet))
- **SharpAESCrypt** - A C# implementation of the AESCrypt file format (see [SharpAESCrypt](https://github.com/kenkendk/sharpaescrypt))
- **Moq** - The most popular and friendly mocking framework for .NET (see [Moq](https://github.com/Moq/moq4))
- **SemanticTypes** - Support for implementing semantic types (see [SemanticTypes](https://github.com/mperdeck/semantictypes))
