using PayPalCheckoutSdk.Core;
using PayPalCheckoutSdk.Orders;
using PayPalHttp;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using System.Linq;

namespace TropiNailsPro.Services
{
    public class PayPalService
    {
        private readonly IConfiguration _config;
        private readonly IHttpContextAccessor _http;

        // 🔥 PRECIO OFICIAL
        // Precio oficial mensual TropiNails Pro
// USD $10.20 ≈ RD$600
private const decimal PRECIO_OFICIAL = 10.20m;

        public PayPalService(
            IConfiguration config,
            IHttpContextAccessor http
        )
        {
            _config = config;
            _http = http;
        }

        // =====================================================
        // 🔥 CLIENTE PAYPAL
        // =====================================================

        private PayPalHttpClient Client()
        {
            var clientId = _config["PayPal:ClientId"];
            var secret = _config["PayPal:Secret"];

            // 🔥 PRODUCCIÓN
            var environment = new LiveEnvironment(
                clientId,
                secret
            );

            return new PayPalHttpClient(environment);
        }

        // =====================================================
        // 🔥 CREAR ORDEN
        // =====================================================

        public async Task<string> CrearOrden(
            decimal monto,
            string descripcion
        )
        {
            try
            {
                var request = new OrdersCreateRequest();

                request.Prefer("return=representation");

                var baseUrl =
                    $"{_http.HttpContext.Request.Scheme}://" +
                    $"{_http.HttpContext.Request.Host}";

                var requestBody = new OrderRequest()
                {
                    CheckoutPaymentIntent = "CAPTURE",

                    PurchaseUnits = new List<PurchaseUnitRequest>()
                    {
                        new PurchaseUnitRequest()
                        {
                            Description = descripcion,

                            AmountWithBreakdown =
                                new AmountWithBreakdown()
                                {
                                    CurrencyCode = "USD",
                                    Value = monto.ToString("F2")
                                }
                        }
                    },

                    ApplicationContext = new ApplicationContext()
                    {
                        BrandName = "TropiNails Pro",

                        LandingPage = "LOGIN",

                        UserAction = "PAY_NOW",

                        ReturnUrl =
                            $"{baseUrl}/Suscripcion/Exito",

                        CancelUrl =
                            $"{baseUrl}/Suscripcion/Cancelado"
                    }
                };

                request.RequestBody(requestBody);

                var response = await Client().Execute(request);

                var result = response.Result<Order>();

                return result.Id;

                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    "ERROR CREAR ORDEN PAYPAL: " + ex.Message
                );

                return null;
            }
        }

        // =====================================================
        // 🔥 CAPTURAR ORDEN
        // =====================================================

        public async Task<bool> CapturarOrden(string orderId)
{
try
{
// 🔥 VALIDAR ORDERID
if (string.IsNullOrEmpty(orderId))
return false;


    // =====================================================
    // 🔥 CAPTURAR ORDEN
    // =====================================================

    var request =
        new OrdersCaptureRequest(orderId);

    request.RequestBody(
        new OrderActionRequest()
    );

    var response =
        await Client().Execute(request);

        Console.WriteLine("==============");
Console.WriteLine("BODY PAYPAL");
Console.WriteLine("==============");

    var result = response.Result<Order>();

    Console.WriteLine("STATUS: " + result.Status);

    Console.WriteLine("ID: " + result.Id);

Console.WriteLine(
    "PURCHASE UNITS NULL: " +
    (result.PurchaseUnits == null)
);

if (result.PurchaseUnits != null)
{
    Console.WriteLine(
        "PURCHASE UNITS COUNT: " +
        result.PurchaseUnits.Count
    );
}

    // =====================================================
    // 🔥 VALIDAR STATUS
    // =====================================================

    if (result.Status != "COMPLETED")
    {
        Console.WriteLine(
            "STATUS INCORRECTO: " +
            result.Status
        );

        return false;
    }

    // =====================================================
    // 🔥 VALIDAR PURCHASE UNIT
    // =====================================================

    var purchaseUnit =
        result.PurchaseUnits?.FirstOrDefault();

        Console.WriteLine("==============");
Console.WriteLine("RESULT COMPLETO");
Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(
    result,
    new System.Text.Json.JsonSerializerOptions
    {
        WriteIndented = true
    }
));
Console.WriteLine("==============");

    if (purchaseUnit == null)
    {
        Console.WriteLine(
            "PURCHASE UNIT NULL"
        );

        return false;
    }

    // =====================================================
    // 🔥 VALIDAR MONTO
    // =====================================================

    var capture = purchaseUnit.Payments?.Captures?.FirstOrDefault();

if (capture == null)
{
    Console.WriteLine("CAPTURE NULL");
    return false;
}

var amount = capture.Amount;

if (amount == null)
{
    Console.WriteLine("AMOUNT NULL EN CAPTURE");
    return false;
}

if (amount == null)
{
    Console.WriteLine("AMOUNT NULL");

    Console.WriteLine(
        "PAYMENTS NULL: " +
        (purchaseUnit.Payments == null)
    );

    return false;
}

Console.WriteLine(
    "MONEDA: " +
    amount.CurrencyCode
);

Console.WriteLine(
    "MONTO: " +
    amount.Value
);

    // 🔥 VALIDAR MONEDA
    if (amount.CurrencyCode != "USD")
    {
        Console.WriteLine(
            "MONEDA INVALIDA"
        );

        return false;
    }

    // 🔥 VALIDAR MONTO EXACTO
    if (!decimal.TryParse(
        amount.Value,
        out decimal montoPagado))
    {
        Console.WriteLine(
            "NO SE PUDO CONVERTIR EL MONTO"
        );

        return false;
    }

    if (
        Math.Round(montoPagado, 2)
        !=
        Math.Round(PRECIO_OFICIAL, 2)
    )
    {
        Console.WriteLine(
            "MONTO INVALIDO"
        );

        Console.WriteLine(
            "PAGADO: " +
            montoPagado
        );

        Console.WriteLine(
            "ESPERADO: " +
            PRECIO_OFICIAL
        );

        return false;
    }

    // =====================================================
    // 🔥 TODO CORRECTO
    // =====================================================

    Console.WriteLine(
        "PAGO VALIDADO CORRECTAMENTE"
    );

    return true;
}
catch (Exception ex)
{
    Console.WriteLine("==============");
    Console.WriteLine("ERROR PAYPAL");
    Console.WriteLine(ex.ToString());
    Console.WriteLine("==============");

    if (
        ex.ToString()
        .Contains("ORDER_ALREADY_CAPTURED")
    )
    {
        Console.WriteLine(
            "ORDEN YA CAPTURADA"
        );

        return true;
    }

    return false;
}


}

        }
    }
