using Dapper;
using Microsoft.Data.SqlClient;

const string Cs =
    @"Server=(localdb)\mssqllocaldb;Database=MailingListDapperDb;Integrated Security=true;TrustServerCertificate=true;";
await EnsureSchemaAsync(Cs);

using var connection = new SqlConnection(Cs);
connection.Open();

for (;;)
{
    Console.WriteLine(@"1 Insert buyer  2 Insert country  3 Insert city  4 Insert section  5 Insert promo product
6 Update buyer  7 Update country  8 Update city  9 Update section 10 Update promo product
11 Delete buyer 12 Delete country 13 Delete city 14 Delete section 15 Delete promo product
16 Cities by country 17 Sections by buyer 18 Promo by section  0 Exit");
    Console.Write("Choice: ");
    var line = Console.ReadLine();
    if (line == "0") break;
    switch (line)
    {
        case "1":
            InsertBuyer(connection); break;
        case "2":
            InsertCountry(connection); break;
        case "3":
            InsertCity(connection); break;
        case "4":
            InsertSection(connection); break;
        case "5":
            InsertPromo(connection); break;
        case "6":
            UpdateBuyer(connection); break;
        case "7":
            UpdateCountry(connection); break;
        case "8":
            UpdateCity(connection); break;
        case "9":
            UpdateSection(connection); break;
        case "10":
            UpdatePromo(connection); break;
        case "11":
            DeleteBuyer(connection); break;
        case "12":
            DeleteCountry(connection); break;
        case "13":
            DeleteCity(connection); break;
        case "14":
            DeleteSection(connection); break;
        case "15":
            DeletePromo(connection); break;
        case "16":
            ListCitiesByCountry(connection); break;
        case "17":
            ListSectionsByBuyer(connection); break;
        case "18":
            ListPromoBySection(connection); break;
        default:
            Console.WriteLine("Unknown"); break;
    }
}

static async Task EnsureSchemaAsync(string cs)
{
    var master = new SqlConnection(
        @"Server=(localdb)\mssqllocaldb;Database=master;Integrated Security=true;TrustServerCertificate=true;");
    await master.OpenAsync();
    await master.ExecuteAsync(
        "IF DB_ID(N'MailingListDapperDb') IS NULL CREATE DATABASE MailingListDapperDb;");
    master.Close();

    await using var conn = new SqlConnection(cs);
    await conn.OpenAsync();

    await conn.ExecuteAsync(
        "IF OBJECT_ID(N'dbo.Countries',N'U') IS NULL CREATE TABLE dbo.Countries (Id INT IDENTITY PRIMARY KEY, Name NVARCHAR(200) NOT NULL);" +
        "IF OBJECT_ID(N'dbo.Cities',N'U') IS NULL CREATE TABLE dbo.Cities (Id INT IDENTITY PRIMARY KEY, Name NVARCHAR(200) NOT NULL, CountryId INT NOT NULL REFERENCES dbo.Countries(Id));" +
        "IF OBJECT_ID(N'dbo.Buyers',N'U') IS NULL CREATE TABLE dbo.Buyers (Id INT IDENTITY PRIMARY KEY, FullName NVARCHAR(200) NOT NULL, Email NVARCHAR(260) NOT NULL, CityId INT NOT NULL REFERENCES dbo.Cities(Id));" +
        "IF OBJECT_ID(N'dbo.Sections',N'U') IS NULL CREATE TABLE dbo.Sections (Id INT IDENTITY PRIMARY KEY, Title NVARCHAR(200) NOT NULL);" +
        "IF OBJECT_ID(N'dbo.BuyerSections',N'U') IS NULL CREATE TABLE dbo.BuyerSections (BuyerId INT NOT NULL REFERENCES dbo.Buyers(Id) ON DELETE CASCADE, SectionId INT NOT NULL REFERENCES dbo.Sections(Id) ON DELETE CASCADE, PRIMARY KEY(BuyerId, SectionId));" +
        "IF OBJECT_ID(N'dbo.PromoProducts',N'U') IS NULL CREATE TABLE dbo.PromoProducts (Id INT IDENTITY PRIMARY KEY, Title NVARCHAR(300) NOT NULL, Price DECIMAL(18,2) NOT NULL, SectionId INT NOT NULL REFERENCES dbo.Sections(Id));");
}

static void InsertBuyer(SqlConnection connection)
{
    Console.WriteLine("Cities:");
    foreach (var c in connection.Query<CityDto>("SELECT Id, Name FROM dbo.Cities ORDER BY Id"))
        Console.WriteLine($"{c.Id} {c.Name}");
    Console.Write("FullName: ");
    var fullName = Console.ReadLine()!.Trim();
    Console.Write("Email: ");
    var email = Console.ReadLine()!.Trim();
    Console.Write("CityId: ");
    if (!int.TryParse(Console.ReadLine(), out var cityId))
    {
        Console.WriteLine("Bad CityId"); return;
    }
    Console.Write("SectionIds comma-separated or empty: ");
    var idsRaw = Console.ReadLine()?.Trim();
    var ids = string.IsNullOrEmpty(idsRaw)
        ? Array.Empty<int>()
        : idsRaw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(s => int.TryParse(s, out var z) ? z : -1).Where(z => z > 0).ToArray();
    var buyerId = connection.ExecuteScalar<int>(
        "INSERT INTO dbo.Buyers(FullName, Email, CityId) OUTPUT INSERTED.Id VALUES(@fn,@em,@cid);",
        new { fn = fullName, em = email, cid = cityId });
    foreach (var sid in ids)
        connection.Execute("INSERT INTO dbo.BuyerSections(BuyerId, SectionId) VALUES(@bid,@sid);",
            new { bid = buyerId, sid });
    Console.WriteLine($"BuyerId={buyerId}");
}

static void InsertCountry(SqlConnection connection)
{
    Console.Write("Country Name: ");
    var n = Console.ReadLine()!.Trim();
    var id = connection.ExecuteScalar<int>("INSERT INTO dbo.Countries(Name) OUTPUT INSERTED.Id VALUES(@n);", new { n });
    Console.WriteLine($"CountryId={id}");
}

static void InsertCity(SqlConnection connection)
{
    Console.WriteLine("Countries:");
    foreach (var c in connection.Query<CountryDto>("SELECT Id, Name FROM dbo.Countries ORDER BY Id"))
        Console.WriteLine($"{c.Id} {c.Name}");
    Console.Write("City Name: ");
    var name = Console.ReadLine()!.Trim();
    Console.Write("CountryId: ");
    if (!int.TryParse(Console.ReadLine(), out var countryId))
    {
        Console.WriteLine("Bad CountryId"); return;
    }
    var id = connection.ExecuteScalar<int>(
        "INSERT INTO dbo.Cities(Name, CountryId) OUTPUT INSERTED.Id VALUES(@name,@countryId);",
        new { name, countryId });
    Console.WriteLine($"CityId={id}");
}

static void InsertSection(SqlConnection connection)
{
    Console.Write("Section Title: ");
    var t = Console.ReadLine()!.Trim();
    var id = connection.ExecuteScalar<int>("INSERT INTO dbo.Sections(Title) OUTPUT INSERTED.Id VALUES(@t);", new { t });
    Console.WriteLine($"SectionId={id}");
}

static void InsertPromo(SqlConnection connection)
{
    Console.WriteLine("Sections:");
    foreach (var s in connection.Query<SectionDto>("SELECT Id, Title FROM dbo.Sections ORDER BY Id"))
        Console.WriteLine($"{s.Id} {s.Title}");
    Console.Write("Promo Title: ");
    var title = Console.ReadLine()!.Trim();
    Console.Write("Price (InvariantCulture decimal): ");
    if (!decimal.TryParse(Console.ReadLine(), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var price))
    {
        Console.WriteLine("Bad price"); return;
    }
    Console.Write("SectionId: ");
    if (!int.TryParse(Console.ReadLine(), out var sectionId))
    {
        Console.WriteLine("Bad SectionId"); return;
    }
    var id = connection.ExecuteScalar<int>(
        "INSERT INTO dbo.PromoProducts(Title, Price, SectionId) OUTPUT INSERTED.Id VALUES(@title,@price,@sectionId);",
        new { title, price, sectionId });
    Console.WriteLine($"PromoId={id}");
}

static void UpdateBuyer(SqlConnection connection)
{
    Console.WriteLine("Buyers:");
    foreach (var b in connection.Query<BuyerDto>("SELECT Id, FullName, Email, CityId FROM dbo.Buyers ORDER BY Id"))
        Console.WriteLine($"{b.Id} {b.FullName} {b.Email} city={b.CityId}");
    Console.Write("Buyer Id: ");
    if (!int.TryParse(Console.ReadLine(), out var id))
    {
        Console.WriteLine("Bad Id"); return;
    }
    Console.Write("FullName: ");
    var fullName = Console.ReadLine()!.Trim();
    Console.Write("Email: ");
    var email = Console.ReadLine()!.Trim();
    Console.Write("CityId: ");
    if (!int.TryParse(Console.ReadLine(), out var cityId))
    {
        Console.WriteLine("Bad CityId"); return;
    }
    connection.Execute("UPDATE dbo.Buyers SET FullName=@fn, Email=@em, CityId=@cid WHERE Id=@id;",
        new { fn = fullName, em = email, cid = cityId, id });
    Console.WriteLine("Done");
}

static void UpdateCountry(SqlConnection connection)
{
    Console.Write("Country Id: ");
    if (!int.TryParse(Console.ReadLine(), out var id))
    {
        Console.WriteLine("Bad Id"); return;
    }
    Console.Write("Name: ");
    var name = Console.ReadLine()!.Trim();
    connection.Execute("UPDATE dbo.Countries SET Name=@name WHERE Id=@id;", new { name, id });
    Console.WriteLine("Done");
}

static void UpdateCity(SqlConnection connection)
{
    Console.Write("City Id: ");
    if (!int.TryParse(Console.ReadLine(), out var id))
    {
        Console.WriteLine("Bad Id"); return;
    }
    Console.Write("Name: ");
    var name = Console.ReadLine()!.Trim();
    Console.Write("CountryId: ");
    if (!int.TryParse(Console.ReadLine(), out var cid))
    {
        Console.WriteLine("Bad CountryId"); return;
    }
    connection.Execute("UPDATE dbo.Cities SET Name=@name, CountryId=@cid WHERE Id=@id;",
        new { name, cid, id });
    Console.WriteLine("Done");
}

static void UpdateSection(SqlConnection connection)
{
    Console.Write("Section Id: ");
    if (!int.TryParse(Console.ReadLine(), out var id))
    {
        Console.WriteLine("Bad Id"); return;
    }
    Console.Write("Title: ");
    var t = Console.ReadLine()!.Trim();
    connection.Execute("UPDATE dbo.Sections SET Title=@t WHERE Id=@id;", new { t, id });
    Console.WriteLine("Done");
}

static void UpdatePromo(SqlConnection connection)
{
    Console.Write("Promo Id: ");
    if (!int.TryParse(Console.ReadLine(), out var id))
    {
        Console.WriteLine("Bad Id"); return;
    }
    Console.Write("Title: ");
    var title = Console.ReadLine()!.Trim();
    Console.Write("Price: ");
    if (!decimal.TryParse(Console.ReadLine(), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var price))
    {
        Console.WriteLine("Bad price"); return;
    }
    Console.Write("SectionId: ");
    if (!int.TryParse(Console.ReadLine(), out var sid))
    {
        Console.WriteLine("Bad SectionId"); return;
    }
    connection.Execute(
        "UPDATE dbo.PromoProducts SET Title=@title, Price=@price, SectionId=@sid WHERE Id=@id;",
        new { title, price, sid, id });
    Console.WriteLine("Done");
}

static void DeleteBuyer(SqlConnection connection)
{
    Console.Write("Buyer Id: ");
    if (!int.TryParse(Console.ReadLine(), out var id))
    {
        Console.WriteLine("Bad"); return;
    }
    connection.Execute("DELETE FROM dbo.Buyers WHERE Id=@id;", new { id });
    Console.WriteLine("Done");
}

static void DeleteCountry(SqlConnection connection)
{
    Console.Write("Country Id: ");
    if (!int.TryParse(Console.ReadLine(), out var id))
    {
        Console.WriteLine("Bad"); return;
    }
    connection.Execute(
        @"DELETE bs FROM dbo.BuyerSections bs
JOIN dbo.Buyers b ON bs.BuyerId=b.Id
JOIN dbo.Cities c ON b.CityId=c.Id WHERE c.CountryId=@id;
DELETE FROM dbo.Buyers WHERE CityId IN (SELECT Id FROM dbo.Cities WHERE CountryId=@id);
DELETE FROM dbo.Cities WHERE CountryId=@id;
DELETE FROM dbo.Countries WHERE Id=@id;", new { id });
    Console.WriteLine("Done");
}

static void DeleteCity(SqlConnection connection)
{
    Console.Write("City Id: ");
    if (!int.TryParse(Console.ReadLine(), out var id))
    {
        Console.WriteLine("Bad"); return;
    }
    connection.Execute("DELETE FROM dbo.BuyerSections WHERE BuyerId IN (SELECT Id FROM dbo.Buyers WHERE CityId=@id); DELETE FROM dbo.Buyers WHERE CityId=@id; DELETE FROM dbo.Cities WHERE Id=@id;",
        new { id });
    Console.WriteLine("Done");
}

static void DeleteSection(SqlConnection connection)
{
    Console.Write("Section Id: ");
    if (!int.TryParse(Console.ReadLine(), out var id))
    {
        Console.WriteLine("Bad"); return;
    }
    connection.Execute("DELETE FROM dbo.BuyerSections WHERE SectionId=@id; DELETE FROM dbo.PromoProducts WHERE SectionId=@id; DELETE FROM dbo.Sections WHERE Id=@id;",
        new { id });
    Console.WriteLine("Done");
}

static void DeletePromo(SqlConnection connection)
{
    Console.Write("Promo Id: ");
    if (!int.TryParse(Console.ReadLine(), out var id))
    {
        Console.WriteLine("Bad"); return;
    }
    connection.Execute("DELETE FROM dbo.PromoProducts WHERE Id=@id;", new { id });
    Console.WriteLine("Done");
}

static void ListCitiesByCountry(SqlConnection connection)
{
    Console.Write("Country Id: ");
    if (!int.TryParse(Console.ReadLine(), out var countryId))
    {
        Console.WriteLine("Bad"); return;
    }
    var rows = connection.Query<CityDto>(
        "SELECT Id, Name FROM dbo.Cities WHERE CountryId=@countryId ORDER BY Name;", new { countryId });
    foreach (var r in rows) Console.WriteLine($"{r.Id} {r.Name}");
}

static void ListSectionsByBuyer(SqlConnection connection)
{
    Console.Write("Buyer Id: ");
    if (!int.TryParse(Console.ReadLine(), out var buyerId))
    {
        Console.WriteLine("Bad"); return;
    }
    var rows = connection.Query<SectionDto>(@"
SELECT s.Id, s.Title FROM dbo.Sections s
JOIN dbo.BuyerSections bs ON bs.SectionId=s.Id
WHERE bs.BuyerId=@buyerId
ORDER BY s.Title;", new { buyerId });
    foreach (var r in rows) Console.WriteLine($"{r.Id} {r.Title}");
}

static void ListPromoBySection(SqlConnection connection)
{
    Console.Write("Section Id: ");
    if (!int.TryParse(Console.ReadLine(), out var sectionId))
    {
        Console.WriteLine("Bad"); return;
    }
    var rows = connection.Query<PromoDto>(
        "SELECT Id, Title, Price, SectionId FROM dbo.PromoProducts WHERE SectionId=@sectionId ORDER BY Title;",
        new { sectionId });
    foreach (var r in rows) Console.WriteLine($"{r.Id} {r.Title} {r.Price} section={r.SectionId}");
}

internal sealed record CountryDto { public int Id { get; init; } public string Name { get; init; } = ""; }
internal sealed record CityDto { public int Id { get; init; } public string Name { get; init; } = ""; }
internal sealed record SectionDto { public int Id { get; init; } public string Title { get; init; } = ""; }
internal sealed record BuyerDto { public int Id { get; init; } public string FullName { get; init; } = ""; public string Email { get; init; } = ""; public int CityId { get; init; } }
internal sealed record PromoDto { public int Id { get; init; } public string Title { get; init; } = ""; public decimal Price { get; init; } public int SectionId { get; init; } }
