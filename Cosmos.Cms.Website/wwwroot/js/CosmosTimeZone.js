$(document).ready(function () {
    $("#timeZone").html(getLocalTimeZone());
});

function getLocalTimeZone() {
    var datetime = new Date();
    var dateTimeString = datetime.toString();
    var timezone = dateTimeString.substring(dateTimeString.indexOf("(") - 1);
    return timezone;
}


