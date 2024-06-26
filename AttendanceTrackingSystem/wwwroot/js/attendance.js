﻿
/*==========================================================*/
/* partial View */
/*==========================================================*/
function loadStudentsAttendancePartial() {
    $.ajax({
        url: '/Attendance/AttendanceStudent/' ,
        type: 'GET',
        success: function (result) {
            $("#UserContainer").empty();
            $("#UserContainer").html(result);
           
        },
        error: function (xhr, status, error) {
            console.error('AJAX Error:', status, error);
            alert('Error occurred while fetching data.');
        }
    });
}
function loadUserAttendancePartial(Type) {
    $.ajax({
        url: '/Attendance/UserAttendance/',
        type: 'GET',
        data: { Type: Type },
        success: function (result) {
            $("#UserContainer").empty();
            $("#UserContainer").html(result);
           
        },
        error: function (xhr, status, error) {
            console.error('AJAX Error:', status, error);
            alert('Error occurred while fetching data.');
        }
    });
}


function getRandomLightColor() {
    var letters = '89ABCDEF'; // Light colors range
    var color = '#';
    for (var i = 0; i < 6; i++) {
        color += letters[Math.floor(Math.random() * letters.length)];
    }
    return color;
}

function setRandomColors() {
    $('.attendance-item').each(function () {
        var randomColor = getRandomLightColor();
        console.log(randomColor); 
        $(this).css('background-color', randomColor); 
    });
}
//var currentDate = new Date();
//var options = {  year: 'numeric', month: 'long', day: 'numeric' };
//var formattedDate = currentDate.toLocaleDateString('en-US', options);

//document.getElementById('date').textContent = formattedDate;
//document.getElementById('day').textContent = currentDate.toLocaleDateString('en-US', { weekday: 'long' });
  
// Search functionality
$(document).ready(function () {
    setRandomColors();
    $("#searchInput").hide();
    $('.tracksDropdown').on('change', function () {
        var selectedTrackId = $(this).val(); // Get the value of the selected option
        loadStudentsPartial(selectedTrackId); // Call the function with the selected ID
    });

    $("#searchInput").on("keyup", function () {
        var value = $(this).val().toLowerCase();
        $("#instructorTable tbody tr").filter(function () {
            $(this).toggle($(this).text().toLowerCase().indexOf(value) > -1)
        });
        $("#attendanceList .attendance-item").filter(function () {
            $(this).toggle($(this).text().toLowerCase().indexOf(value) > -1)
        });
    });

   
    document.getElementById('userType').addEventListener('change', function () {
        var selectedType = $(this).val();
        if (selectedType == "Instructor") {
            loadUserAttendancePartial(selectedType);

        } else if (selectedType == "Student") {
            

            loadStudentsAttendancePartial();   

        }else if (selectedType == "Employee") {
            loadUserAttendancePartial(selectedType);

        }else if (selectedType == "0") {
        loadStudentsAttendancePartial();
    }
    });
});