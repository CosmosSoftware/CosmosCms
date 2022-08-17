var Customers = 0;
var TotalCustomers = 0;
var SDGEOuttages = 0;
var PSPSOuttages = 0;
var Outtages = 0;
var FireNumber = 0;
var FireAcreage = [];
var CustArray = [];
var FireGeometries = [];
var view;


require([
        "esri/Map",
        "esri/views/MapView",
        "esri/layers/GraphicsLayer",
        "esri/tasks/QueryTask",
        "esri/tasks/support/Query",
        "esri/geometry/Point",
        "esri/widgets/Legend",
        "esri/Graphic",
        "esri/layers/FeatureLayer",
        "esri/widgets/Locate",
        "esri/widgets/Search",
        "esri/symbols/PictureMarkerSymbol",
        "esri/geometry/geometryEngine"
    ],
    function(Map,
        MapView,
        GraphicsLayer,
        QueryTask,
        Query,
        Point,
        Legend,
        Graphic,
        FeatureLayer,
        Locate,
        Search,
        PictureMarkerSymbol,
        geometryEngine) {

        // Make sure page is set to default setting on page refresh
        document.getElementById("ReportHead").style.display = "none";
        document.getElementById("LocationReport").innerHTML = " ";
        document.getElementById("nearestIncName").innerHTML = "";
        resetAqiFeilds();

        // Put Layer over map so mobile can scroll
        var theMapParent = document.getElementById("viewDiv").parentElement;
        var theMapDiv = document.getElementById("viewDiv");
        var theMapDivCover2 = document.createElement("div");
        theMapDivCover2.id = "esrimap_canvas_cover2";
        theMapDivCover2.style =
            "cursor: default; height: 300px;width: 100%; position:absolute; top:0; bottom:0; left:0; right:0;margin-top:100px;display:block;z-index:2;opacity:0.0; filter: alpha(opacity = 0.0);background:transparent;background-color:#CCCCCC;z-index: 3;";
        theMapParent.insertBefore(theMapDivCover2, theMapDiv);

        // Get Fire Info from Endpoint
        var Firexhr = new XMLHttpRequest();
        var Fireurl =
            "https://services3.arcgis.com/T4QMspbfLg3qTGWY/ArcGIS/rest/services/Public_Wildfire_Perimeters_View/FeatureServer/0/query?where=UnitID+like%27CA%25%27&outFields=*&returnGeometry=false&f=pjson";
        Firexhr.open("GET", Fireurl, false);
        Firexhr.send();
        var Firedata = Firexhr.responseText;
        var Fireobj = JSON.parse(Firedata);

        // Process Fire Info and Display Results
        if (Fireobj.features) {
            FireNumber = Fireobj.features.length;
            for (x = 0; x < FireNumber; x++) {
                FireAcreage.push(Fireobj.features[x].attributes.GISAcres);
            }

            var Acreagesum = FireAcreage.reduce(function(a, b) {
                    return a + b;
                },
                0);

            var Custsum = CustArray.reduce(function(a, b) {
                    return a + b;
                },
                0);
            var Acreage = Acreagesum;


            document.getElementById("WildfiresBurning").innerHTML =
                (FireNumber).toString().replace(/(\d)(?=(\d{3})+(?!\d))/g, "$1,");
            document.getElementById("AcresBurned").innerHTML =
                (Acreage.toFixed(0)).toString().replace(/(\d)(?=(\d{3})+(?!\d))/g, "$1,");
        }

        // Create Map
        var map = new Map({
            basemap: "dark-gray"
        });

        // Create Fire Layer
        var WF = new FeatureLayer({
            url:
                "https://services3.arcgis.com/T4QMspbfLg3qTGWY/ArcGIS/rest/services/Public_Wildfire_Perimeters_View/FeatureServer/0/",
            title: "Wildfire"
        });

        // Create PSPS Layer
        var PSPS = new FeatureLayer({
            url:
                "https://services.arcgis.com/BLN4oKB0N1YSgvY8/arcgis/rest/services/Statewide_PSPS_Current_Active_Outage_Areas_(Public)/FeatureServer/0",
            title: "PSPS",
            definitionExpression: "'Status='De-Energized'"
        });

        // Create San Diego Layer
        var SDGE = new FeatureLayer({
            url:
                " https://sempra.maps.arcgis.com/home/signin.html?returnUrl=https%3A%2F%2Fsempra.maps.arcgis.com%2Fhome%2Fitem.html%3Fid%3D4e12d32187ca49eda9403b953f06397a",
            title: "SDGE",
            definitionExpression: "'Status='De-Energized'"
        });

        // Create Buffer and Point Layers
        var bufferLayer = new GraphicsLayer();
        var pointLayer = new GraphicsLayer();

        // Create View
        view = new MapView({
            map: map,
            container: "viewDiv",
            center: [-120.174, 38.255],
            zoom: 7,
            ui: {
                components: ["attribution"]
            }
        });

        // Stop Map from moving
        view.on("key-down",
            function(event) {
                var prohibitedKeys = ["+", "-", "Shift", "_", "="];
                var keyPressed = event.key;
                if (prohibitedKeys.indexOf(keyPressed) !== -1) {
                    event.stopPropagation();
                }
            });
        view.on("mouse-wheel",
            function(event) {
                event.stopPropagation();
            });
        view.on("double-click",
            function(event) {
                event.stopPropagation();
            });
        view.on("double-click",
            ["Control"],
            function(event) {
                event.stopPropagation();
            });
        view.on("drag",
            ["Shift"],
            function(event) {
                event.stopPropagation();
            });
        view.on("drag",
            function(event) {
                event.stopPropagation();
            });
        view.on("drag",
            ["Shift", "Control"],
            function(event) {
                event.stopPropagation();
            });

        // Add Legend to View
        var legend = new Legend({
            view: view
        });

        // Create Search Widget
        var searchWidget = new Search({
            view: view,
            resultGraphicEnabled: false,
            popupEnabled: false
        });

        // Add Search to View
        view.ui.add(searchWidget,
            {
                position: "top-left"
            });

        // Upon doing a search Display header and perform bufferPoint Actions
        searchWidget.on("search-complete",
            function(event) {
                document.getElementById("ReportHead").style.display = "block";
                document.getElementById("LocationReport").innerHTML =
                    " " + event.results[0].results[0].feature.attributes.Match_addr;
                var geom = event.results[0].results[0].feature.geometry;
                bufferPoint(geom);
            });


        // Display Information based on geometry location obtained.
        function bufferPoint(point) {

            // Clear stuff
            clearGraphics();

            // Center on selected point
            view.center = [point.longitude, point.latitude];

            // Zoom on selected point
            view.zoom = 10;

            // Add Legend to Map
            view.ui.add(legend, "bottom-left");

            // Add Layers to Map
            map.addMany([bufferLayer, pointLayer, PSPS, SDGE, WF]);

            // Set HTML ELments to Default Values  
            document.getElementById("PowerOutage").innerHTML = "No";
            document.getElementById("WildfiresBurning").innerHTML = "None";
            document.getElementById("AcresBurned").innerHTML = "None";
            document.getElementById("wildfireText").innerHTML = "Active Wildfires:";
            document.getElementById("acresBurnedText").innerHTML = "Acres Burned:";
            document.getElementById("nearestIncName").innerHTML = "";
            document.getElementById("fireMapLink").href =
                "https://california.maps.arcgis.com/apps/webappviewer/index.html?id=cc900c7fbed44ce98365a08835cef6cf";

            // Create Symbol
            var polySym = {
                type: "simple-fill",
                color: [112, 146, 190, 0.5],
                outline: {
                    color: [0, 0, 0, 0.5],
                    width: 2
                }
            };

            // Create Marker
            var pointSym = {
                type: "picture-marker",
                url: "/Images/location-whte-100.png",
                width: "32px",
                height: "32px"
            };

            // Add Symbol and Marker
            pointLayer.add(
                new Graphic({
                    geometry: point,
                    symbol: pointSym
                })
            );

            // Build Buffer
            var buffer = geometryEngine.geodesicBuffer(point, 20, "miles");

            // Add Buffer to Buffer Layer
            bufferLayer.add(
                new Graphic({
                    geometry: buffer,
                    symbol: polySym
                })
            );

            // Create Fire Query
            var fireQuery = new Query();
            fireQuery.geometry = buffer;
            fireQuery.outFields = ["*"];
            fireQuery.spatialRelationship = "intersects";

            // Create Fire Task
            var FireTask = new QueryTask({
                url:
                    "https://services3.arcgis.com/T4QMspbfLg3qTGWY/ArcGIS/rest/services/Public_Wildfire_Perimeters_View/FeatureServer/0"
            });

            // Perform Fire Task
            FireTask.execute(fireQuery).then(function(results) {
                document.getElementById("wildfireText").innerHTML = "Active Wildfires Near Me:";
                document.getElementById("acresBurnedText").innerHTML = "Acres Burned Near Me:";
                document.getElementById("fireMapLink").href =
                    "https://california.maps.arcgis.com/apps/webappviewer/index.html?id=cc900c7fbed44ce98365a08835cef6cf&marker=" +
                    point.longitude +
                    "," +
                    point.latitude +
                    "&level=12";

                if (results.features) {
                    for (x = 0; x < results.features.length; x++) {
                        document.getElementById("WildfiresBurning").innerHTML = (results.features.length).toString()
                            .replace(/(\d)(?=(\d{3})+(?!\d))/g, "$1,");
                        document.getElementById("AcresBurned").innerHTML =
                            (results.features[x].attributes.GISAcres.toFixed(0)).toString()
                            .replace(/(\d)(?=(\d{3})+(?!\d))/g, "$1,");
                        document.getElementById("nearestIncName").innerHTML =
                            "(" + results.features[x].attributes.IncidentName + ")";
                    }
                }
            });

            // Create AQI Query
            var aqiQuery = new Query();
            aqiQuery.geometry = buffer;
            aqiQuery.outFields = ["*"];
            aqiQuery.spatialRelationship = "intersects";

            // orderByFields

            // Create AQI Task
            var AQITask = new QueryTask({
                url:
                    "https://services.arcgis.com/cJ9YHowT8TU7DUyn/ArcGIS/rest/services/Air%20Now%20Current%20Monitor%20Data%20Public/FeatureServer/0"
            });

            // Perform AQI Task
            AQITask.execute(aqiQuery).then(function(results) {

                console.log(results);

                if (results.features) {
                    if (results.features.length > 0) {
                        var aqiLevelText = "";
                        var aqiLevelColor = "";
                        var aqiLevelForecolor = "";

                        var aqiNumber = results.features[0].attributes.PM_AQI;

                        if (!(aqiNumber === null)) {
                            if ((aqiNumber > -1) & (aqiNumber < 51)) {
                                aqiLevelText = "Good";
                                aqiLevelColor = "00e400";
                                aqiLevelForecolor = "000000";
                            } else {
                                if ((aqiNumber > 50) & (aqiNumber < 101)) {
                                    aqiLevelText = "Moderate";

                                    aqiLevelColor = "f1d800"; //ffff00 e8e800 f6d600
                                    aqiLevelForecolor = "000000";
                                } else {
                                    if ((aqiNumber > 100) & (aqiNumber < 151)) {
                                        aqiLevelText = "Unhealthy for Sensitive Groups";
                                        aqiLevelColor = "ff7e00";
                                        aqiLevelForecolor = "FFFFFF";
                                    } else {
                                        if ((aqiNumber > 150) & (aqiNumber < 201)) {
                                            aqiLevelText = "Unhealthy";
                                            aqiLevelColor = "ff0000";
                                            aqiLevelForecolor = "FFFFFF";
                                        } else {
                                            if ((aqiNumber > 200) & (aqiNumber < 301)) {
                                                aqiLevelText = "Very Unhealthy";
                                                aqiLevelColor = "8f3f97";
                                                aqiLevelForecolor = "FFFFFF";
                                            } else {
                                                aqiLevelText = "Hazardous";
                                                aqiLevelColor = "7e0023";
                                                aqiLevelForecolor = "FFFFFF";
                                            }
                                        }
                                    }
                                }
                            }

                            document.getElementById("aqiText").innerHTML = "Air Quality Index:";
                            document.getElementById("aqiNumber").innerHTML = aqiNumber;
                            document.getElementById("aqiText2").innerHTML =
                                "<div class=\"progress-bar progress-bar-striped progress-bar-animated p-0 m-0\" role=\"progressbar\" style=\"width: 100%; border-radius:3px; height:15px; background-color: #" +
                                aqiLevelColor +
                                ";\" aria-valuenow=\"25\" aria-valuemin=\"0\" aria-valuemax=\"100\"></div>";
                            document.getElementById("aqiButton").innerHTML =
                                "<a class=\"h4 btn btn-secondary rounded-50 p-x-md line-height-1-2em\" href=\"https://www.airnow.gov/aqi/aqi-basics/\"><div style=\"width:100%; border:3px solid #" +
                                aqiLevelColor +
                                "; padding-top:5px;padding-bottom: 5px;border-radius: 25px;padding-left:10px;padding-right: 10px;\">" +
                                aqiLevelText +
                                "</div><div>Learn about Your Air Quailty</div></a>";
                        } else {
                            resetAqiFeilds();
                        }
                    } else {
                        resetAqiFeilds();
                    }
                } else {
                    resetAqiFeilds();
                }
            });


            // Create PSPS and San Diego Query
            var powerQuery = new Query();
            powerQuery.geometry = buffer;
            powerQuery.outFields = ["*"];
            powerQuery.spatialRelationship = "intersects";
            powerQuery.where = "Status='De-Energized'";

            //Create Power Outages Task
            var PowerTask = new QueryTask({
                url:
                    "https://services.arcgis.com/BLN4oKB0N1YSgvY8/ArcGIS/rest/services/Power_Outages_(View)/FeatureServer/1"
            });

            //Perform Power Outages Task
            PowerTask.execute(powerQuer2y).then(function(results) {
                if (results.features.length > 0) {
                    document.getElementById("PowerOutage").innerHTML = "Yes";
                } else {
                    document.getElementById("PowerOutage").innerHTML = "No";
                }
            });

/*
	    // Create San Diego Query
    	var query2 = new Query();
    	query2.geometry = buffer;
    	query2.outFields = ["*"];
    	query2.spatialRelationship = "intersects";
    	query2.where = "'Status='De-Energized'";

        //Create San Diego Task
        var SDGETask = new QueryTask({
            url: "https://services.arcgis.com/S0EUI1eVapjRPS5e/ArcGIS/rest/services/Event_Outages_PSPS_Public_View/FeatureServer/0"
        });
        
    	var SDGEOut = 0;
    	var SDGECust = 0;

        //Perform San Diego Task
        SDGETask.execute(query2).then(function (results) {
            if(results.features){
                for (x = 0; x < results.features.length; x++) {
                    SDGEOut = results.features.length;
                    SDGECust = results.features[x].attributes.TOTALCUST;
                }
            }
        });
*/
        }

        // Set up Clear Graphic Activity    		 
        function clearGraphics() {
            pointLayer.removeAll();
            bufferLayer.removeAll();
        }

        // Function to resent HTML Fields
        function resetAqiFeilds() {
            document.getElementById("aqiText").innerHTML = "Air Quality Index";
            document.getElementById("aqiNumber").innerHTML = "";
            document.getElementById("aqiText2").innerHTML = "";
            document.getElementById("aqiButton").innerHTML =
                "<a class=\"h4 btn btn-secondary rounded-50 p-x-md line-height-1-2em\" href=\"https://www.airnow.gov/aqi/aqi-basics/\">Learn about Air Quality Index</a>";
        };
    });