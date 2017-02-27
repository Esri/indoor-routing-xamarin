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
Since this is a multi-platform application, the app settings are stored inside an xml that is created when the app is first installed and updated throughout the app's usage. AppSetting can be changed to include additional settings per developer requirements. Using the factory pattern, a static instance of CurrentSettings is made available throughout the application.

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
<center><img src="Assets/AppStartCheck.PNG" width="300"  /></center>

If a download is needed, the app uses the iOS [NSUrlSessionDownloadTask](https://github.com/xamarin/ios-samples/blob/master/SimpleBackgroundTransfer/SimpleBackgroundTransfer/SimpleBackgroundTransferViewController.cs) to download the mmpk. This insures that the mmpk is downloaded even if the user switches away from the app during the download.

```csharp
// Create a new download task.
var downloadTask = this.session.CreateDownloadTask(NSUrl.FromString(downloadUrl));
```
Both the mmpk and the app settings files are stored on the device and once downloaded, the app can work in a fully disconnected environment.

### Loading the Map
<center><img src="Assets/InitialExtent.PNG" width="300" /></center>

Once everything is downloaded, the mmpk and all of its contents are loaded inside the Initialize method of the Main View Model.
```csharp
internal async Task InitializeAsync()
{
    // Load mmpk from device and load it
    var mmpk = await this.LoadMMPKAsync().ConfigureAwait(false);

    // Display map from the mmpk. Assumption is made that the first map of the mmpk is the one used
    this.Map = mmpk.Maps.FirstOrDefault();
    await Map.LoadAsync().ConfigureAwait(false);

    // Set the locator to be used in the app
    var locator = mmpk.LocatorTask;
    await locator.LoadAsync().ConfigureAwait(false);

    // Create instance of the Location View Model
    if (LocationViewModel.Instance == null)
    {
        LocationViewModel.Instance = LocationViewModel.Create(Map, locator);
    }

    // Set viewpoint of the map depending on user's setting
    await this.SetInitialViewPointAsync().ConfigureAwait(false);
}
```
The Initial Viewpoint is the initial extent that the map is loaded to when the application starts. This extent is configured in the settings. If the initial extent is not set, the map will display the full extent of the mmpk.

### Navigating the Map

There are three ways to move around the map: pan, zoom and floor change. The floor picker is displayed only when the map is zoomed in past a certain extent. This extent is also defined in the settings and can be easily changed. When the floor picker is activated, the app applies [definition expressions](https://developers.arcgis.com/net/latest/ios/api-reference/) on the rooms and walls layers to only display the selected floor.

<center><img src="Assets/ManualZoomIn.PNG" width="300" /></center>

```csharp
for (int i = 1; i < Map.OperationalLayers.Count; i++)
{
    ...
    // select chosen floor
    featureLayer.DefinitionExpression = string.Format(
                        "{0} = '{1}'",
                        AppSettings.CurrentSettings.RoomsLayerFloorColumnName,
                    this.SelectedFloorLevel);
    ...
}
```
### Search Functionality

The mmpk locator is used to search for offices or employees. When user starts entering text, the locator is used to provide suggestions.

<center><img src="Assets/AutocompleteOffice.PNG" width="300" /> <img src="Assets/AutocompleteName.PNG" width="300" /></center>

```csharp
internal async Task<IReadOnlyList<SuggestResult>> GetLocationSuggestionsAsync(string userInput)
{
    var locatorInfo = this.Locator.LocatorInfo;

    if (locatorInfo.SupportsSuggestions)
    {
        // restrict the search to return no more than 10 suggestions
        var suggestParams = new SuggestParameters { MaxResults = 10 };

        // get suggestions for the text provided by the user
        var suggestions = await this.Locator.SuggestAsync(userInput, suggestParams);
        return suggestions;
    }
    return null;
}
```
When a suggestion is selected, the selection is then passed to the locator again, this time to retrieve the actual location

```csharp
internal async Task<GeocodeResult> GetSearchedLocationAsync(string searchString)
{
    // Geocode location and return the best match from the list
    var matches = await this.Locator.GeocodeAsync(searchString);
    var bestMatch = matches.FirstOrDefault();
    return bestMatch;
}
```
Several matches are returned, with corresponding accuracy scores. This app makes the assumption that the first match is the best, but it could be changed so users see more than one of the returned matches.
[Read more about locators](https://developers.arcgis.com/net/latest/wpf/guide/search-for-places-geocoding-.htm).

The resulting location is then added to the map inside a [Graphics Overlay](https://developers.arcgis.com/net/latest/wpf/guide/add-graphics-overlays-to-your-app.htm)

<center><img src="Assets/SearchUsingSearchBar.PNG" width="300" /></center>

```csharp
if (geocodeResult != null)
{
    // create a picture marker symbol and offset it
    var uiImagePin = UIImage.FromBundle("MapPin");
    var mapPin = this.ImageToByteArray(uiImagePin);
    var roomMarker = new PictureMarkerSymbol(new RuntimeImage(mapPin));
    roomMarker.OffsetY = uiImagePin.Size.Height * 0.65;

    // Create graphic
    var mapPinGraphic = new Graphic(geocodeResult.DisplayLocation, roomMarker);

    // Add pin to map
    var graphicsOverlay = this.MapView.GraphicsOverlays["PinsGraphicsOverlay"];
    graphicsOverlay.Graphics.Clear();
    graphicsOverlay.Graphics.Add(mapPinGraphic);
    this.MapView.GraphicsOverlays["PinsGraphicsOverlay"].IsVisible = true;
}
```

### Querying Data
The locator provides a map location for the searched feature, but does not provide any attribute data. To populate the card at the bottom of the map and select the appropriate floor, the data is queried

```csharp
// Run query to get attributes of the selected room
var roomsLayer = this.Map.OperationalLayers[AppSettings.CurrentSettings.RoomsLayerIndex] as FeatureLayer;

if (roomsLayer != null)
{
    // Get feature table to be queried
    var roomsTable = roomsLayer.FeatureTable;

    // Set query parameters
    var queryParams = new QueryParameters()
    {
        ReturnGeometry = true,
        WhereClause = string.Format(string.Join(" = '{0}' OR ", AppSettings.CurrentSettings.LocatorFields) + " = '{0}'", searchString)
    };

    // Query the feature table
    var queryResult = await roomsTable.QueryFeaturesAsync(queryParams);
        return queryResult.FirstOrDefault();
    }
}
```
### Setting a Home Location
The app offers the option to set a Home Location. This location is used to pre-populate the starting point of routes, and the user can easily get back Home by tapping the Home button to the right of the search bar. The same locator is used to generate the suggestions and retrieve the selected location. Once set, the Home location is written to the AppSettings file and persists between app sessions.

<center><img src="Assets/SettingsWithHome.PNG" width="300" /> <img src="Assets/GoToHome.PNG" width="300" /></center>

### Routing
Once search is complete and a feature is selected on the map, the route icon appears on the bottom card. Tapping that navigates the app to the routing page, where user is prompted to add a Start and End location. The End location is always pre-populated with the feature selected on the map. The Start location is only pre-populated if Location Services is enabled or if the user has a Home Location set. When the user hits Route Me, the app navigates back to the map and the route is generated using the TransportationNetwork that was packaged in the mmpk. [Read more about routing and how to create routes using transportation networks](https://developers.arcgis.com/net/latest/ios/guide/find-a-route.htm)

```csharp
var routeTask = await RouteTask.CreateAsync(this.Map.TransportationNetworks[0]);

if (routeTask != null)
{
    // Get the default route parameters
    var routeParams = await routeTask.CreateDefaultParametersAsync();

    // Explicitly set values for some params
    // Indoor networks do not support turn by turn navigation
    routeParams.ReturnRoutes = true;
    routeParams.ReturnDirections = true;

    // Create stops
    var startPoint = new Stop(fromLocation);
    var endPoint = new Stop(toLocation);

    // assign the stops to the route parameters
    routeParams.SetStops(new List<Stop> { startPoint, endPoint });

    // Execute routing
    var routeResult = await routeTask.SolveRouteAsync(routeParams);

    return routeResult;
}
```

<center><img src="Assets/SearchFromLocation.PNG" width="300" /></center>

The route is then displayed on the map using a GraphicsOverlay

<center><img src="Assets/FullRoute.PNG" width="300" /></center>

```csharp
// get the route from the results
var newRoute = this.Route.Routes.FirstOrDefault();

// create a picture marker symbol for start pin
var uiImageStartPin = UIImage.FromBundle("StartCircle");
var startPin = this.ImageToByteArray(uiImageStartPin);
var startMarker = new PictureMarkerSymbol(new RuntimeImage(startPin));

// create a picture marker symbol for end pin
var uiImageEndPin = UIImage.FromBundle("EndCircle");
var endPin = this.ImageToByteArray(uiImageEndPin);
var endMarker = new PictureMarkerSymbol(new RuntimeImage(endPin));

// create a graphic to represent the route
var routeSymbol = new SimpleLineSymbol();
routeSymbol.Width = 5;
routeSymbol.Style = SimpleLineSymbolStyle.Solid;
routeSymbol.Color = System.Drawing.Color.FromArgb(127, 18, 121, 193);

var routeGraphic = new Graphic(newRoute.RouteGeometry, routeSymbol);

// Add graphics to overlay
this.MapView.GraphicsOverlays["RouteGraphicsOverlay"].Graphics.Clear();
this.MapView.GraphicsOverlays["RouteGraphicsOverlay"].Graphics.Add(routeGraphic);
this.MapView.GraphicsOverlays["RouteGraphicsOverlay"].Graphics.Add(startGraphic);
this.MapView.GraphicsOverlays["RouteGraphicsOverlay"].Graphics.Add(endGraphic);

// Pan to the new route
await this.MapView.SetViewpointGeometryAsync(newRoute.RouteGeometry, 30);
```
Zooming, panning and switching floors are enabled during routing, and so is searching or moving to user's Home location.

<center><img src="Assets/RouteFloor1.PNG" width="300" /> <img src="Assets/RouteFloor3.PNG" width="300" /></center>

To clear the Route, simply tap on the map and select the Clear Route option.

<center><img src="Assets/ClearRoute.PNG" width="300" /></center>

### Using Location Services

The app is written to allow using device location for tracking. However, this should be used with caution, as it does not work properly indoors. There are several options available for indoor positioning devices, and developers should to do their research to find the best option for their needs.

```csharp
if (AppSettings.CurrentSettings.IsLocationServicesEnabled == true)
{
    this.MapView.LocationDisplay.IsEnabled = true;
    this.MapView.LocationDisplay.AutoPanMode = LocationDisplayAutoPanMode.Recenter;
    this.MapView.LocationDisplay.InitialZoomScale = 150;
}
```
