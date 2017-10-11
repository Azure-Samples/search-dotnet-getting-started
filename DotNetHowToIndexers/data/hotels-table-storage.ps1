#Requires -Modules AzureRM
Param(
    [Parameter(Mandatory=$true)]
    [string]$StorageAccountName,
    [Parameter(Mandatory=$true)]
    [string]$StorageAccountKey
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$ctx = New-AzureStorageContext -StorageAccountName $StorageAccountName -StorageAccountKey $StorageAccountKey
$hotels = New-AzureStorageTable -Name "hotels" -Context $ctx

function Add-Hotel() {
    [CmdletBinding()]
    param(
        $Properties
    )

    $hotel = New-Object -TypeName Microsoft.WindowsAzure.Storage.Table.DynamicTableEntity
    foreach ($pair in $Properties) {
        $type = $pair.Value.GetType().Name
        if ($type -ne 'string') {
            $hotel[$pair.Name] = (ConvertTo-Json -Compress $pair.Value)
        } else {
            $hotel[$pair.Name] = $pair.Value
        }
    }
    $hotel['IsDeleted'] = 'false'
    $hotel.RowKey = $Properties['HotelId']
    $hotel.PartitionKey = ''
    $hotels.CloudTable.Execute([Microsoft.WindowsAzure.Storage.Table.TableOperation]::Insert($hotel)) | Out-Null
}

$content = (Get-Content (Join-Path $PSScriptRoot 'hotels.json')) |
    ConvertFrom-Json

foreach ($hotel in $content) {
    Add-Hotel $hotel.PSObject.Properties
}
