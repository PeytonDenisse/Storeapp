namespace StoreAPI;

public class Prompts
{
    public static string GenerateOrdersPrompt(string jsonData)
    {
        return $@"
            Eres un analista experto en datos en retail.
            Analiza los siguientes datos de ordenes, productoa y tiendas (en JSON)
            {jsonData}

            Debes responder **exclusivamente** en formato JSON y con esta estructura:
            {{
                ""topProducts"":{{""name"": string, ""unitSold"": int, ""totalRevenue"": double}},
                ""topStore"":{{""name"": string, ""totalSales"": double, ""shareOfTotalSales"": double}},
                ""avgSpending"":double,
                ""patterns"":[string]
            }}
            En el apartado agrega analisis como: cual es la tienda que mas vende,
            que producto son los que mas dinero dejan por orden. 
            Si por alguna razón no puedes generar esta respuesta valida (por ejemplo, te hace faltan datos o algun error en el formato ),  responde **SOLO** con el text: error.
            No me saludes, no me des explicaciones, no me des comentarios y no me incluyas texto adicional

        ";
    }
    public static string GenerateInvoicesPrompt(string jsonData)
    {
        return $@"
            Eres un analista de negocio. Analiza el siguiente **arreglo JSON de facturas** y responde **EXCLUSIVAMENTE** con un **JSON válido** (UTF-8, sin comentarios, sin texto adicional fuera del JSON).

            Datos de entrada (JSON):
            {jsonData}

            Debes devolver **exactamente** esta estructura:

            {{
              ""totalInvoices"": int,
              ""paidInvoices"": int,
              ""unpaidInvoices"": int,
              ""totalRevenue"": double,
              ""averageInvoiceAmount"": double,
              ""commonCurrencies"": [string],
              ""patterns"": [string]
            }}

            Cálculos y criterios:
            - **totalInvoices**: número total de facturas.
            - **paidInvoices**: facturas con `IsPaid == true`.
            - **unpaidInvoices**: facturas con `IsPaid == false`.
            - **totalRevenue**: suma de los importes de las facturas **pagadas**. Usa `Total` si existe, o `Subtotal + Tax` si no.
            - **averageInvoiceAmount**: promedio de importe total considerando todas las facturas (mismo criterio anterior).
            - **commonCurrencies**: monedas más frecuentes (ordenadas por frecuencia descendente).
            - **patterns**: observaciones útiles como:
              - ""X% de facturas están pagadas.""
              - ""La moneda más utilizada es <MONEDA>.""
              - ""Se detectan montos atípicos o variaciones por fecha (si aplica)."" 

            Reglas de salida:
            - Devuelve **solo** el JSON, sin texto adicional.
            - Usa **punto** como separador decimal.
            - Si no puedes construir el JSON (datos insuficientes, error en formato, etc.), responde **únicamente** con:
            error
            ";
    }
    
    
}