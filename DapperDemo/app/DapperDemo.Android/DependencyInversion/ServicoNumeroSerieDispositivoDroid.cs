using Android.App;
using System.Diagnostics;
using System.Globalization;

namespace DapperDemo.Android.DependencyInversion
{
    public class ServicoNumeroSerieDispositivoDroid
    {
        public string RecuperarNumeroSerieDispositivo()
        {
            string numeroSerie = global::Android.Provider.Settings.Secure.GetString(Application.Context.ContentResolver, global::Android.Provider.Settings.Secure.AndroidId)!;

            Debug.WriteLine(string.Format(CultureInfo.InvariantCulture, "[{0}-{1}] numeroSerie={2}", "ServicoNumeroSerieDispositivoDroid", "RecuperarNumeroSerieDispositivo", numeroSerie));
            return numeroSerie;
        }
    }
}