
using DAL;

// יצירת השרת והגדרת השירותים של האפליקציה.
var builder = WebApplication.CreateBuilder(args);

// הוספת תמיכה ב-Controllers ובתיעוד OpenAPI.
builder.Services.AddControllers();
builder.Services.AddOpenApi();

// שליפת מחרוזת החיבור למסד הנתונים והוספת שם מסד הנתונים.
string dbConnectionString =
    (builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found."))
    + "Database=RoniRoseDB;";

// רישום UserDB במערכת באמצעות Singleton, כך שאותו מופע משותף לאורך חיי האפליקציה.
builder.Services.AddSingleton<DAL.UserDB>(_ =>
    new DAL.UserDB(dbConnectionString));

// רישום OrderDB באמצעות Scoped, כך שנוצר מופע חדש לכל בקשת HTTP.
builder.Services.AddScoped<DAL.OrderDB>(_ =>
    new DAL.OrderDB(dbConnectionString));

// רישום ProductDB באמצעות Scoped, כך שנוצר מופע חדש לכל בקשת HTTP.
builder.Services.AddScoped<ProductDB>(_ =>
    new ProductDB(dbConnectionString));

// יצירת האפליקציה לאחר הגדרת השירותים.
var app = builder.Build();

// שליפת מחרוזת חיבור לשרת לצורך אתחול מסד הנתונים.
string serverConnectionString =
    builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

// הגדרת צינור הבקשות (Middleware) של השרת.
if (app.Environment.IsDevelopment())
{
    // הפעלת תיעוד OpenAPI בסביבת פיתוח בלבד.
    app.MapOpenApi();
}

// אתחול מסד הנתונים ויצירת הטבלאות או הנתונים הנדרשים.
await DAL.DatabaseInitializer.InitializeAsync(serverConnectionString);

// הפניית בקשות HTTP לחיבור מאובטח באמצעות HTTPS.
app.UseHttpsRedirection();

// הפעלת מנגנון הרשאות המשתמשים.
app.UseAuthorization();

// חיבור ה-Controllers לכתובות ה-API של האפליקציה.
app.MapControllers();

// הפעלת השרת והתחלת קבלת בקשות.
app.Run();
