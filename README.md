# DokanCloudFS
**DokanCloudFS** is a virtual filesystem for various publicly accessible Cloud storage services on the Microsoft Windows platform.

## Objective

**DokanCloudFS** implements a virtual filesystem that allows direct mounting of various publicly accessible Cloud storage services on the Microsoft Windows platform.

Mounted Cloud storage drives appear as ordinary removable drives in Windows Explorer (albeit very slow ones) and can be used just about like any local drive or file share. All content written through DokanCloudFS is transparently encrypted on the Cloud storage backend.

Some limitations apply concerning transfer speed, maximum file size, permissions, and alternate streams depending on parameters of the Cloud storage service used as a backend.

## Features

- Mounting of Cloud storage volumes as removable drives into Windows Explorer
  - number of mounted volumes only limited by available drive letters
  - mounting of multiple accounts of the same Cloud storage service in parallel
- Full support for interactive file manipulation in Winows Explorer including but not limited to
  - copying and moving of multiple files and folders between DokanCloudFS drives and other drives
  - opening of files in default application via Mouse double click
  - right-click-menu on drive, files, and folders with access to menu commands and drive/file properties
  - thumbnails display for media files
- transparent encryption and decryption of all file content via AESCrypt
  - encryption key configurable per drive

## Supported Cloud storage services

DokanCloudFS requires a gateway assembly for any Cloud storage service to be used as a backend.

The expected gateway interface types and a set of prefabricated gateways can be taken from the GitHub repository of the related **CloudFS** project located at https://github.com/viciousviper/CloudFS.

## System Requirements

- Platform
  - .NET 4.6
- Drivers
  - Dokan driver 0.8.0 or greater (see https://github.com/dokan-dev/dokany/releases)
- Operating system
  - tested on Windows 8.1 x64 and Windows Server 2012 R2
  - expected to run on Windows 7/8/8.1/10 and Windows Server 2008(R2)/2012(R2)

## Local compilation

- Compile the CloudFS solution (see https://github.com/viciousviper/CloudFS)
- copy all assemblies from the //Gateways// directory in the build output of //CloudFS.GatewayTests// to the //Library// directory of DokanCloudFS
- Compile the DokanCloudFS solution
- Check that the //Gateways// directory in the build output of //DokanCloudFS.Mounter// contains all desired gateway assemblies and their dependencies (e.g. Newtonsoft.JSON.dll)

## Usage

- Configure the desired mount points in DokanCloudFS.Mounter's configuration file
- Run //IgorSoft.DokanCloudFS.Mounter.exe// from the command line

## Remarks

Dueto the nature of the Dokan kernel-mode drivers involved, any severe errors inside DokanCloudFS can result in Windows Bluescreens.

**Don't install this project on produktion environments.**

## Release Notes

2015-12-30 Version 1.0.0.0 - Initial release. This version has not been extensively tested apart from the OneDrive gateway - **expect bugs**.

## Future plans

- improve stability
- improve performance
- allow alternate encryption schemes
- allow read-access to unencrypted content on Cloud storage volumes
- allow mounting and unmounting of individual drives without restarting the mounter process

## References

DokanCloudFS would not have been possible without the expertise and great effort devoted by their respective creators to the following projects:

- **Dokany** - The User mode file system library for windows (see http://dokan-dev.github.io for the entire Dokany ecosystem, https://github.com/dokan-dev/dokany for the specific driver)
- **Dokan.NET** - The Dokan DotNET wrapper (see https://github.com/dokan-dev/dokan-dotnet)
- **SharpAESCrypt** - A C# implementation of the AESCrypt file format (see https://github.com/kenkendk/sharpaescrypt)
- **Moq** - The most popular and friendly mocking framework for .NET (see https://github.com/Moq/moq4)
- **SemanticTypes** - Support for implementing semantic types (see https://github.com/mperdeck/semantictypes)