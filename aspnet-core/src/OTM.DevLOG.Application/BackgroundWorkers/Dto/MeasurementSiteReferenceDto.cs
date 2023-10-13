using System;
namespace OTM.DevLOG.BackgroundWorkers.Dto
{
	public class MeasurementSiteReferenceDto
	{
        public Measurementsiterecord measurementSiteRecord { get; set; }
    }

    public class Measurementsiterecord
    {
        public string id { get; set; }
        public string version { get; set; }
        public DateTime measurementSiteRecordVersionTime { get; set; }
        public string computationMethod { get; set; }
        public string measurementEquipmentReference { get; set; }
        public Measurementequipmenttypeused measurementEquipmentTypeUsed { get; set; }
        public Measurementsitename measurementSiteName { get; set; }
        public string measurementSiteNumberOfLanes { get; set; }
        public string measurementSide { get; set; }
        public Measurementspecificcharacteristic[] measurementSpecificCharacteristics { get; set; }
        public Measurementsitelocation measurementSiteLocation { get; set; }
    }

    public class Measurementequipmenttypeused
    {
        public Values values { get; set; }
    }

    public class Values
    {
        public Value value { get; set; }
    }

    public class Value
    {
        public string lang { get; set; }
        public string text { get; set; }
    }

    public class Measurementsitename
    {
        public Values1 values { get; set; }
    }

    public class Values1
    {
        public Value1 value { get; set; }
    }

    public class Value1
    {
        public string lang { get; set; }
        public string text { get; set; }
    }

    public class Measurementsitelocation
    {
        public string xsitype { get; set; }
        public Locationfordisplay locationForDisplay { get; set; }
        public Supplementarypositionaldescription supplementaryPositionalDescription { get; set; }
        public Alertcpoint alertCPoint { get; set; }
        public Pointextension pointExtension { get; set; }
    }

    public class Locationfordisplay
    {
        public string latitude { get; set; }
        public string longitude { get; set; }
    }

    public class Supplementarypositionaldescription
    {
        public Affectedcarriagewayandlanes affectedCarriagewayAndLanes { get; set; }
    }

    public class Affectedcarriagewayandlanes
    {
        public string carriageway { get; set; }
    }

    public class Alertcpoint
    {
        public string xsitype { get; set; }
        public string alertCLocationCountryCode { get; set; }
        public string alertCLocationTableNumber { get; set; }
        public string alertCLocationTableVersion { get; set; }
        public Alertcdirection alertCDirection { get; set; }
        public Alertcmethod4primarypointlocation alertCMethod4PrimaryPointLocation { get; set; }
    }

    public class Alertcdirection
    {
        public string alertCDirectionCoded { get; set; }
    }

    public class Alertcmethod4primarypointlocation
    {
        public Alertclocation alertCLocation { get; set; }
        public Offsetdistance offsetDistance { get; set; }
    }

    public class Alertclocation
    {
        public string specificLocation { get; set; }
    }

    public class Offsetdistance
    {
        public string offsetDistance { get; set; }
    }

    public class Pointextension
    {
        public Openlrextendedpoint openlrExtendedPoint { get; set; }
    }

    public class Openlrextendedpoint
    {
        public Openlrpointlocationreference openlrPointLocationReference { get; set; }
    }

    public class Openlrpointlocationreference
    {
        public Openlrgeocoordinate openlrGeoCoordinate { get; set; }
        public Openlrpointalongline openlrPointAlongLine { get; set; }
    }

    public class Openlrgeocoordinate
    {
        public Openlrcoordinate openlrCoordinate { get; set; }
    }

    public class Openlrcoordinate
    {
        public string latitude { get; set; }
        public string longitude { get; set; }
    }

    public class Openlrpointalongline
    {
        public string openlrSideOfRoad { get; set; }
        public string openlrOrientation { get; set; }
        public string openlrPositiveOffset { get; set; }
        public Openlrlocationreferencepoint openlrLocationReferencePoint { get; set; }
        public Openlrlastlocationreferencepoint openlrLastLocationReferencePoint { get; set; }
    }

    public class Openlrlocationreferencepoint
    {
        public Openlrcoordinate1 openlrCoordinate { get; set; }
        public Openlrlineattributes openlrLineAttributes { get; set; }
        public Openlrpathattributes openlrPathAttributes { get; set; }
    }

    public class Openlrcoordinate1
    {
        public string latitude { get; set; }
        public string longitude { get; set; }
    }

    public class Openlrlineattributes
    {
        public string openlrFunctionalRoadClass { get; set; }
        public string openlrFormOfWay { get; set; }
        public string openlrBearing { get; set; }
    }

    public class Openlrpathattributes
    {
        public string openlrLowestFRCToNextLRPoint { get; set; }
        public string openlrDistanceToNextLRPoint { get; set; }
    }

    public class Openlrlastlocationreferencepoint
    {
        public Openlrcoordinate2 openlrCoordinate { get; set; }
        public Openlrlineattributes1 openlrLineAttributes { get; set; }
    }

    public class Openlrcoordinate2
    {
        public string latitude { get; set; }
        public string longitude { get; set; }
    }

    public class Openlrlineattributes1
    {
        public string openlrFunctionalRoadClass { get; set; }
        public string openlrFormOfWay { get; set; }
        public string openlrBearing { get; set; }
    }

    public class Measurementspecificcharacteristic
    {
        public string index { get; set; }
        public Measurementspecificcharacteristics measurementSpecificCharacteristics { get; set; }
    }

    public class Measurementspecificcharacteristics
    {
        public string accuracy { get; set; }
        public string period { get; set; }
        public string specificLane { get; set; }
        public string specificMeasurementValueType { get; set; }
        public Specificvehiclecharacteristics specificVehicleCharacteristics { get; set; }
    }

    public class Specificvehiclecharacteristics
    {
        public object lengthCharacteristic { get; set; }
        public string vehicleType { get; set; }
    }
}

