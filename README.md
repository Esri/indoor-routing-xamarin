# Indoor Routing with Xamarin for iOS
Find your way around indoor spaces.

## Description
Route and track indoors using custom building data, indoor network and locators. The example app uses Esri's Redlands Campus data, indoor network and locators to find offices or employees and route between indoor spaces.

The example application is open source and available on GitHub. Developers can modify it to use their own data and custom locators.

## Preparing the Data
A large portion of getting this application off the ground is gathering and preparing the data. The app uses a [Mobile Map Package](http://pro.arcgis.com/en/pro-app/help/sharing/overview/mobile-map-package.htm) (mmpk) which contains all the needed base data, feature data, network data and locators.

### Base Data
While base data is not needed for the application to run, it's good to have for visual appeal, so the buildings do not look like they're floating on a grid. Mobile Map Packages do not support including a [Tile Package](http://desktop.arcgis.com/en/arcmap/latest/map/working-with-arcmap/about-tile-packages.htm) (tpk) or an [Online Basemap](http://doc.arcgis.com/en/arcgis-online/create-maps/choose-basemap.htm). To add a basemap into the mmpk, a developer's best option is to create a [Vector Tile Package](http://pro.arcgis.com/en/pro-app/help/sharing/overview/vector-tile-package.htm) (vtpk) of their base data and include it in the mmpk. If creating a vtpk is not a viable option, then the code can be modified to download a tpk to use as a basemap. Or, if the application is to be used in a connected environment, then adding an online basemap to the app might be the fastest and simplest option.

```csharp
// Sample code on how to add basemap to a map
// create a new (empty) map
var myMap = new Map();
// create a streets basemap
var streetsBasemap = Basemap.CreateStreets();
// add the basemap to the map
myMap.Basemap = streetsBasemap;
// add layers from the mmpk
...
```
### Feature Data
In this app, the [feature](http://support.esri.com/sitecore/content/support/Home/other-resources/gis-dictionary/term/feature) data represents building rooms and building walls data. They are included in the mmpk as [Feature Layers](http://support.esri.com/sitecore/content/support/Home/other-resources/gis-dictionary/term/feature%20layer). In the application, these layers are referred to as Operational Layers and are used to display building information and run queries. Both the feature layers and the network data were created using the [CAD to GIS](http://www.arcgis.com/home/item.html?id=66cd6ea44302402c9eaad7ae0ad2bf72) set of tools developed by Esri's Professional Services group.
### Network Data
The [Network Dataset](http://support.esri.com/sitecore/content/support/Home/other-resources/gis-dictionary/term/network%20dataset) is used to generate the routes between offices. While this app was built to support only one network, it could be modified to accommodate multiple networks. For example, if buildings are far apart and users would need to drive and walk to get between offices, a second, road network could be added. Once the network data is created, it should be ready to be used in the app. However, [a set of tools](http://pro.arcgis.com/en/pro-app/tool-reference/network-analyst/an-overview-of-the-network-dataset-toolset.htm) in ArcGIS Pro can help with changes needed to be made to the network.

### Locators
Locators provide the ability to find a location based on an address (geocode). In the case of indoor data, the address is an office or an employee's name. To accommodate searching for both office number and employee name, two separate [Single Field Locators](http://pro.arcgis.com/en/pro-app/help/data/geocoding/create-a-locator.htm) were created and then merged into a [Composite Locator](http://pro.arcgis.com/en/pro-app/help/data/geocoding/create-a-composite-locator.htm). The Composite Locator was then added to the mmpk.

## App Architecture
The Indoor Routing is currently a Xamarin iOS app. The business logic is separate from the UI and is stored in a set of ViewModels in the shared part of the solution. This will make it relatively easy to add an Android or UWP UI to it later on. The native architecture was preferred over using Xamarin Forms due to stability concerns and the desire to have a native looking UI. For more information on Xamarin, including how to get started, please see [Microsoft's website](https://developer.xamarin.com/guides/cross-platform/getting_started/).

## The App in Action

### App Settings
Since this is a multi-platform application, the app settings are stored inside an xml that is created when the app is first installed and updated throughout the app's usage. AppSetting can be changed to include additional settings per developer requirements. Through the factory pattern, a static instance of CurrentSettings is available throughout the application.

```csharp
internal static async Task<AppSettings> CreateAsync(string filePath)
{
    var appSettings = new AppSettings();
    appSettings.PortalItemID = "52346d5fc4c348589f976b6a279ec3e6";
    appSettings.PortalItemName = "RedlandsCampus.mmpk";
    appSettings.MmpkDownloadDate = new DateTime(1900, 1, 1);

    ...

    return appSettings;
}
```


### Mobile Map Package Download
 When the app first starts, it checks to see if an mmpk has been downloaded, or if there's an updated version to be downloaded from Portal.
 ```csharp
 // Get portal item
var portal = await ArcGISPortal.CreateAsync().ConfigureAwait(false);
var item = await PortalItem.CreateAsync(portal, AppSettings.CurrentSettings.PortalItemID).ConfigureAwait(false);

// Test if mmpk is not already downloaded or is older than current portal version
if (!this.Files.Contains(this.TargetFileName) ||
    item.Modified.LocalDateTime > AppSettings.CurrentSettings.MmpkDownloadDate)
{
    this.IsDownloading = true;
    this.DownloadURL = item.Url.AbsoluteUri + "/data";
}
else
{
    this.IsReady = true;
}
 ```

[App Start Image here]

If a download is needed, the app uses the iOS [NSUrlSessionDownloadTask](https://github.com/xamarin/ios-samples/blob/master/SimpleBackgroundTransfer/SimpleBackgroundTransfer/SimpleBackgroundTransferViewController.cs) to download the mmpk. This insures that the mmpk is downloaded even if the user switches away from the app during the download.

```csharp
// Create a new download task.
var downloadTask = this.session.CreateDownloadTask(NSUrl.FromString(downloadUrl));
```
