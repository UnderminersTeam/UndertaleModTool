using System.Reflection;
using System.Resources;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Windows;

// Ogólne informacje o zestawie są kontrolowane poprzez następujący 
// zestaw atrybutów. Zmień wartości tych atrybutów, aby zmodyfikować informacje
// powiązane z zestawem.
[assembly: AssemblyTitle("UndertaleModTool")]
[assembly: AssemblyDescription("")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("")]
[assembly: AssemblyProduct("UndertaleModTool")]
[assembly: AssemblyCopyright("Copyright ©  2018")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

// Ustawienie elementu ComVisible na wartość false sprawia, że typy w tym zestawie są niewidoczne
// dla składników COM. Jeśli potrzebny jest dostęp do typu w tym zestawie z
// COM, ustaw wartość true dla atrybutu ComVisible tego typu.
[assembly: ComVisible(false)]

//Aby rozpocząć kompilację aplikacji możliwych do zlokalizowania, ustaw
//<UICulture>Kultura_używana_podczas_kodowania</UICulture> w pliku csproj
//w grupie <PropertyGroup>. Jeśli na przykład jest używany język angielski (USA)
//w plikach źródłowych ustaw dla elementu <UICulture> wartość en-US. Następnie usuń komentarz dla
//poniższego atrybutu NeutralResourceLanguage. Zaktualizuj wartość „en-US” w
//poniższej linii tak, aby dopasować ustawienie UICulture w pliku projektu.

//[assembly: NeutralResourcesLanguage("en-US", UltimateResourceFallbackLocation.Satellite)]


[assembly: ThemeInfo(
    ResourceDictionaryLocation.None, //gdzie znajdują się słowniki zasobów specyficznych dla motywów
                                     //(używane, jeśli nie można odnaleźć zasobu na stronie,
                                     // lub słowniki zasobów aplikacji)
    ResourceDictionaryLocation.SourceAssembly //gdzie znajduje się słownik zasobów ogólnych
                                              //(używane, jeśli nie można odnaleźć zasobu na stronie,
                                              // aplikacji lub słowników zasobów specyficznych dla motywów)
)]


// Informacje o wersji zestawu zawierają następujące cztery wartości:
//
//      Wersja główna
//      Wersja pomocnicza
//      Numer kompilacji
//      Rewizja
//
// Możesz określić wszystkie wartości lub użyć domyślnych numerów kompilacji i poprawki
// przy użyciu symbolu „*”, tak jak pokazano poniżej:
// [assembly: AssemblyVersion("1.0.*")]
[assembly: AssemblyVersion("0.1.7")]
[assembly: AssemblyFileVersion("0.1.7")]
