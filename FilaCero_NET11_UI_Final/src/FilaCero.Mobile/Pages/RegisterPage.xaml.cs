using Microsoft.Maui.Storage;
using System.Text.RegularExpressions;
using FilaCero.Domain;
using FilaCero.Mobile.Services;

namespace FilaCero.Mobile.Pages;

public partial class RegisterPage : ContentPage
{
    private readonly FilaCeroApi _api;
    private readonly AppState _state;
    private DocumentUpload? _document;

    public RegisterPage(AppState state,FilaCeroApi api)
    {
        InitializeComponent();_state=state;_api=api;
        foreach(var clinic in ClinicNames.All) ClinicPicker.Items.Add(clinic);
        ClinicPicker.SelectedIndex=0;
    }
    private async void ChooseDocument(object sender, EventArgs e)
    {
        try
        {
            var file=await FilePicker.Default.PickAsync();
            if(file is null) return;
            var ext=Path.GetExtension(file.FileName).ToLowerInvariant();
            var mime=ext switch {".pdf"=>"application/pdf",".jpg" or ".jpeg"=>"image/jpeg",".png"=>"image/png",_=>null};
            if(mime is null) throw new InvalidOperationException("Sólo se aceptan archivos PDF, JPG y PNG.");
            await using var input=await file.OpenReadAsync();
            if(input.CanSeek && input.Length>1_048_576) throw new InvalidOperationException("El archivo supera 1 MB.");
            using var data=new MemoryStream();await input.CopyToAsync(data);
            if(data.Length>1_048_576) throw new InvalidOperationException("El archivo supera 1 MB.");
            var safeName=Regex.Replace(file.FileName,@"[^a-zA-Z0-9_\- .()]","_");
            _document=new DocumentUpload(safeName,mime,Convert.ToBase64String(data.ToArray()));
            DocumentLabel.Text=safeName;
        }
        catch(Exception ex) { await DisplayAlert("Documento",ex.Message,"Aceptar"); }
    }
    private async void RegisterClicked(object sender,EventArgs e)
    {
        RegisterButton.IsEnabled=false;
        try
        {
            var input=RegistrationRules.Normalize(new RegistrationRequest(FullNameEntry.Text??"",CurpEntry.Text??"",
                NssEntry.Text??"",PhoneEntry.Text??"",ClinicPicker.SelectedItem?.ToString()??"",_document));
            var issues=RegistrationRules.Validate(input);
            if(issues.Count>0) {await DisplayAlert("Revisa el formulario",string.Join("\n",issues.Values),"Aceptar");return;}
            var result=await _api.RegisterAsync(input);
            await _state.SetRegisteredAsync(result);
            await DisplayAlert("Registro recibido","Continúa con el cuestionario de triaje.","Aceptar");
            (Shell.Current as MainTabsPage)?.Reconnect();
            (Shell.Current as MainTabsPage)?.GoTo(2);
        }
        catch(Exception ex) {await DisplayAlert("Registro",ex.Message,"Aceptar");}
        finally {RegisterButton.IsEnabled=true;}
    }
}
