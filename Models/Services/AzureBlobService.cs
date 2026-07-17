using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Options;
using TropiNailsPro.Settings;

namespace TropiNailsPro.Services
{
    public class AzureBlobService
    {
        private readonly BlobContainerClient _containerClient;


        public AzureBlobService(
            IOptions<AzureBlobStorageSettings> settings)
        {
            var blobServiceClient = new BlobServiceClient(
                settings.Value.ConnectionString
            );

            _containerClient = blobServiceClient.GetBlobContainerClient(
                settings.Value.ContainerName
            );
        }


        // ==========================================
        // SUBIR ARCHIVO A AZURE BLOB STORAGE
        // ==========================================
        public async Task<string> SubirArchivoAsync(
            Stream archivo,
            string nombreArchivo,
            string tipoContenido)
        {
            await _containerClient.CreateIfNotExistsAsync(
                PublicAccessType.Blob
            );


            var blobClient = _containerClient.GetBlobClient(
                nombreArchivo
            );


            await blobClient.UploadAsync(
                archivo,
                new BlobHttpHeaders
                {
                    ContentType = tipoContenido
                }
            );


            return blobClient.Uri.ToString();
        }


// ==========================================
// SUBIR ARCHIVO DENTRO DE UNA CARPETA
// ==========================================
public async Task<string> SubirArchivoCarpetaAsync(
    Stream archivo,
    string carpeta,
    string nombreArchivo,
    string tipoContenido)
{
    await _containerClient.CreateIfNotExistsAsync(
        PublicAccessType.Blob
    );


    string rutaArchivo =
        $"{carpeta}/{nombreArchivo}";


    var blobClient =
        _containerClient.GetBlobClient(
            rutaArchivo
        );


    await blobClient.UploadAsync(
        archivo,
        new BlobHttpHeaders
        {
            ContentType = tipoContenido
        }
    );


    return blobClient.Uri.ToString();
}

        // ==========================================
        // ELIMINAR ARCHIVO DE AZURE
        // ==========================================
        public async Task EliminarArchivoAsync(
            string nombreArchivo)
        {
            var blobClient = _containerClient.GetBlobClient(
                nombreArchivo
            );


            await blobClient.DeleteIfExistsAsync();
        }


        // ==========================================
        // OBTENER URL DEL ARCHIVO
        // ==========================================
        public string ObtenerUrlArchivo(
            string nombreArchivo)
        {
            var blobClient = _containerClient.GetBlobClient(
                nombreArchivo
            );


            return blobClient.Uri.ToString();
        }


        // ==========================================
        // PROBAR CONEXIÓN AZURE
        // ==========================================
        public async Task<bool> ProbarConexionAsync()
        {
            try
            {
                await _containerClient.CreateIfNotExistsAsync();

                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}