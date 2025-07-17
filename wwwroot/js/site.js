// Please see documentation at https://docs.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

// Harita yüklendikten sonra boyutunu düzeltmek için
function initializeMap() {
    // Sayfada harita varsa
    if (document.getElementById('map') && typeof L !== 'undefined') {
        console.log("Harita yeniden boyutlandırılıyor...");
        
        // Haritanın görünür olduğundan emin olalım
        document.getElementById('map').style.display = 'block';
        document.getElementById('map').style.height = '500px';
        document.getElementById('map').style.width = '100%';
        
        // Haritayı güncelleyelim
        setTimeout(function() {
            if (typeof L !== 'undefined' && L.map && L.map._onResize) {
                var mapEls = document.querySelectorAll('.leaflet-container');
                mapEls.forEach(function(el) {
                    var event = new Event('resize');
                    window.dispatchEvent(event);
                });
            }
        }, 1000);
    }
}

// Sayfa yüklendiğinde haritayı başlat
document.addEventListener('DOMContentLoaded', function() {
    initializeMap();
    
    // Tab değişikliklerinde haritayı düzeltme
    var tabEls = document.querySelectorAll('button[data-bs-toggle="collapse"]');
    tabEls.forEach(function(el) {
        el.addEventListener('click', function() {
            setTimeout(initializeMap, 500);
        });
    });
});
