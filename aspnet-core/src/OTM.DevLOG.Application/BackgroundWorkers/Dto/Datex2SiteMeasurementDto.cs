using System;
namespace OTM.DevLOG.BackgroundWorkers.Dto;


public class Datex2SiteMeasurementDto
{
    public Body Body { get; set; }
}


public class Body
{
    public D2logicalmodel d2LogicalModel { get; set; }
}

public class D2logicalmodel
{
    public string modelBaseVersion { get; set; }
    public Exchange exchange { get; set; }
    public Payloadpublication payloadPublication { get; set; }
}

public class Exchange
{
    public Supplieridentification supplierIdentification { get; set; }
}

public class Supplieridentification
{
    public string country { get; set; }
    public string nationalIdentifier { get; set; }
}

public class Payloadpublication
{
    public string type { get; set; }
    public string lang { get; set; }
    public DateTime publicationTime { get; set; }
    public Publicationcreator publicationCreator { get; set; }
    public Measurementsitetablereference measurementSiteTableReference { get; set; }
    public Headerinformation headerInformation { get; set; }
    public Sitemeasurement[] siteMeasurements { get; set; }
}

public class Publicationcreator
{
    public string country { get; set; }
    public string nationalIdentifier { get; set; }
}

public class Measurementsitetablereference
{
    public string id { get; set; }

    public string version { get; set; }
    public string targetClass { get; set; }
}

public class Headerinformation
{
    public string confidentiality { get; set; }
    public string informationStatus { get; set; }
}

public class Sitemeasurement
{
    public Measurementsitereference measurementSiteReference { get; set; }
    public DateTime measurementTimeDefault { get; set; }
    public Measuredvalue[] measuredValue { get; set; }
}

public class Measurementsitereference
{
    public string id { get; set; }
    public string version { get; set; }
    public string targetClass { get; set; }
}

public class Measuredvalue
{
    public string index { get; set; }
    public Measuredvalue1 measuredValue { get; set; }
}

public class Measuredvalue1
{
    public Basicdata basicData { get; set; }
}

public class Basicdata
{
    public string type { get; set; }
    public Vehicleflow vehicleFlow { get; set; }
    public Averagevehiclespeed averageVehicleSpeed { get; set; }
}

public class Vehicleflow
{
    public string vehicleFlowRate { get; set; }
    public string supplierCalculatedDataQuality { get; set; }
    public string dataError { get; set; }
}

public class Averagevehiclespeed
{
    public string numberOfInputValuesUsed { get; set; }
    public string speed { get; set; }
    public string standardDeviation { get; set; }
    public string supplierCalculatedDataQuality { get; set; }
    public string dataError { get; set; }
}