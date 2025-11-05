import React, { RefObject, useEffect, useRef, useState } from "react";
import { isMobile } from "react-device-detect";
if (isMobile) import("./mobileMapPageStyles.css");

import { useAuthUser } from "react-auth-kit";
import { useTranslation } from "react-i18next";
import MapWithMarkers from "../../../components/mobile/MapWithMarkers";
import CustomTimePicker from "../../../components/mobile/CustomTimePicker";
import CustomDatePicker from "../../../components/mobile/CustomDatePicker";
import { Button, DatePickerRef, PickerRef, Toast, SpinLoading } from "antd-mobile";
import dayjs from "dayjs";
import {
  AddOutline,
  AntOutline,
  CalendarOutline,
  ClockCircleOutline,
  EnvironmentOutline,
  RightOutline,
  UserOutline,
} from "antd-mobile-icons";
import { BASE_URL } from "../../../configs/GeneralApiType";
import { use } from "i18next";

const MapComponent: React.FC = () => {
  const [location, setLocation] = useState<{ lat: number; lng: number }>();
  const [loading, setLoading] = useState(false);
  const [locationLoading, setLocationLoading] = useState(true);
  const { t } = useTranslation(["mappage", "global"]);
  const user = useAuthUser()()?.user;
  const url = `${BASE_URL}/api/v1/activities`;

  const [timePickerVisible, setTimePickerVisible] = useState(false); // State to control the visibility of the Picker
  const [datePickerVisible, setDatePickerVisible] = useState(false); // State to control the visibility of the Picker
  const [selectedTime, setSelectedTime] = useState<string | null>(null); // State to store the selected time
  const [selectedDate, setSelectedDate] = useState<string | null>(null); // State to store the selected date
  const [activities, setActivities] = useState<Activity[]>([]); // State to store the activities fetched from the API

  // Lista de puntos a marcar.
  const locations = [
    { lat: 40.73061, lng: -73.935242 }, // Ejemplo: Nueva York
    { lat: 34.052235, lng: -118.243683 }, // Ejemplo: Los Ángeles
    { lat: 41.878113, lng: -87.629799 }, // Ejemplo: Chicago
  ];

  interface Activity {
    id: string;
    offerId: string;
    ownerId: string;
    title: string;
    description: string;
    dateTime: string;
    peopleLimit: number;
    location: string;
    latitude: number;
    longitude: number;
  }

  const fetchActivity = async () => {
    setLoading(true);
    try {
      const response = await fetch(url);
      if (response.ok) {
        const data: Activity[] = await response.json();
        const validData = data.filter((activity) => {
          return activity.latitude && activity.longitude;
        });
        setActivities(validData);
        console.log(data);
      } else {
        Toast.show({ icon: "fail", content: "Error fetching activities" });
      }
    } catch (error) {
      Toast.show({ icon: "fail", content: "Error fetching activities" });
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchActivity();
  }, []);

  useEffect(() => {
    if (navigator.geolocation) {
      navigator.geolocation.getCurrentPosition(
        (position) => {
          const { latitude, longitude } = position.coords;
          setLocation({ lat: latitude, lng: longitude });
          setLocationLoading(false);
        },
        (error) => {
          console.error("Error obtaining location", error);
          Toast.show({ 
            icon: "fail", 
            content: "Could not get your location. Using default location." 
          });
          // Set a default location (e.g., city center) when geolocation fails
          setLocation({ lat: 43.8563, lng: 18.4131 }); // Sarajevo as default
          setLocationLoading(false);
        },
        { enableHighAccuracy: true, timeout: 10000, maximumAge: 60000 }
      );
    } else {
      console.error("Geolocation is not supported by this browser.");
      Toast.show({ 
        icon: "fail", 
        content: "Geolocation not supported. Using default location." 
      });
      setLocation({ lat: 43.8563, lng: 18.4131 }); // Sarajevo as default
      setLocationLoading(false);
    }
  }, []);

  return (
    <div className="map-conainer">
      <div>
        <div className="map-filters">
          <div className="filter">
            {/* <span>Fecha:</span> */}
            <Button onClick={() => setDatePickerVisible(true)}>
              <div className="button-content">
                <CalendarOutline className="icon" />
                <span className="value">{selectedDate}</span>
              </div>
            </Button>
            <CustomDatePicker
              visible={datePickerVisible}
              setVisibleHandler={setDatePickerVisible}
              onChange={(value) => setSelectedDate(value)}
              defaultValue={dayjs().format(t("global:date_format"))}
            />
          </div>
          <div className="filter">
            {/* <span>Desde:</span> */}
            <Button onClick={() => setTimePickerVisible(true)}>
              <div className="button-content">
                <ClockCircleOutline className="icon" />
                <span className="value">{selectedTime} </span>
              </div>
            </Button>
            <CustomTimePicker
              visible={timePickerVisible}
              setVisibleHandler={setTimePickerVisible}
              onChange={(value) => setSelectedTime(value)}
              defaultValue={dayjs().format("HH:mm")}
            />
          </div>
        </div>
        
        {(loading || locationLoading) && (
          <div style={{ display: 'flex', justifyContent: 'center', margin: '20px 0' }}>
            <SpinLoading />
            <span style={{ marginLeft: '10px' }}>
              {locationLoading ? 'Getting your location...' : 'Loading activities...'}
            </span>
          </div>
        )}
        
        {location && !locationLoading && (
          <MapWithMarkers center={location} activities={activities} />
        )}
      </div>
      <div>
        {/* {location ? (
          <p>
            Your location: Latitude: {location.lat}, Longitude: {location.lng}
          </p>
        ) : (
          <p>Fetching location...</p>
        )} */}
      </div>
    </div>
  );
};

export default MapComponent;
