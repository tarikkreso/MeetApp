import { isMobile } from "react-device-detect";
if (isMobile) import("./mobileActivitiesPageStyles.css");

import React, { useEffect, useState, RefObject } from "react";
import {
  Card,
  Modal,
  SearchBar,
  Button,
  Divider,
  Input,
  Form,
  DatePicker,
  TextArea,
  Stepper,
  SpinLoading,
  List,
  Selector
} from "antd-mobile";
import { useNavigate, useParams } from "react-router-dom";
import {
  AddOutline,
  AntOutline,
  CalendarOutline,
  ClockCircleOutline,
  EditSOutline,
  EnvironmentOutline,
  RightOutline,
  UserOutline,
} from "antd-mobile-icons";
import dayjs from "dayjs";
import { useTranslation } from "react-i18next";
import { useAuthUser } from "react-auth-kit";
import { BASE_URL } from "../../../configs/GeneralApiType";
import message from "antd/es/message";
import type { DatePickerRef } from "antd-mobile/es/components/date-picker";
import { setTokenSourceMapRange } from "typescript";
import {useAuthHeader} from "react-auth-kit";
import FormItem from "antd/es/form/FormItem";

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
  participantCount?: number;
}

interface ActivityForm {
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

interface Person {
  id: string;
  name: string;
  email: string;
}

// Mock data for people - replace this with your actual data
const mockPeople: Person[] = [
  { id: "1", name: "John Doe", email: "john.doe@example.com" },
  { id: "2", name: "Jane Smith", email: "jane.smith@example.com" },
  { id: "3", name: "Mike Johnson", email: "mike.johnson@example.com" },
  { id: "4", name: "Sarah Wilson", email: "sarah.wilson@example.com" },
  { id: "5", name: "David Brown", email: "david.brown@example.com" },
  { id: "6", name: "Lisa Davis", email: "lisa.davis@example.com" },
  { id: "7", name: "Tom Miller", email: "tom.miller@example.com" },
  { id: "8", name: "Anna Garcia", email: "anna.garcia@example.com" },
];

const ActivitiesMobilePage: React.FC = () => {
  const [isModalVisible, setIsModalVisible] = useState(false);
  const [isFormModalVisible, setIsFormModalVisible] = useState(false);
  const [selectedActivity, setSelectedActivity] = useState<Activity>();
  const [newActivity, setNewActivity] = useState<Partial<Activity>>({});
  const { t } = useTranslation(["activitiespage", "global"]);
  const user = useAuthUser()()?.user;
  const [activities, setActivities] = useState<Activity[]>([]);
  const [searchTerm, setSearchTerm] = useState("");
  const [loading, setLoading] = useState(false);
  const [filteredActivities, setFilteredActivities] = useState<Activity[]>([]);
  const [fromDate, setFromDate] = useState<Date | undefined>();
  const [toDate, setToDate] = useState<Date | undefined>();
  const [open, setOpen] =useState(false);
  const [join, setJoined] = useState(false);
  const [addressSuggestions, setAddressSuggestions] = useState<any[]>([]);
  const [showSuggestions, setShowSuggestions] = useState(false);
  const [selectedLocation, setSelectedLocation] = useState<string>("");
  const [selectedPeople, setSelectedPeople] = useState<string[]>([]);
  const [people, setPeople] = useState<Person[]>(mockPeople); // You can replace this with actual fetch
  const navigate = useNavigate();
  const url = `${BASE_URL}/api/v1/activity`;

  const authHeader=useAuthHeader();
  const dateFormatTemp = t("global:date_format");
  const timeFormatTemp = t("global:time_format");
  const dateFormat =
    dateFormatTemp != "date_format" ? dateFormatTemp : "YYYY-MM-DD";
  const timeFormat = timeFormatTemp != "time_format" ? timeFormatTemp : "HH:mm";

  const [form] = Form.useForm<ActivityForm>();

  const fetchUsers = async () => {
    try {
      const response = await fetch(`${BASE_URL}/api/v1/activity/users`);
      if (response.ok) {
        const data: Person[] = await response.json();
        setPeople(data);
      } else {
        console.error("Error fetching users, using mock data");
        setPeople(mockPeople);
      }
    } catch (error) {
      console.error("Error fetching users, using mock data:", error);
      setPeople(mockPeople);
    }
  };

  const fetchActivity = async () => {
    setLoading(true);
    try {
      const response = await fetch(url);
      if (response.ok) {
        const data: Activity[] = await response.json();
        setActivities(data);
      } else {
        message.error("Error fetching activities");
      }
    } catch (error) {
      message.error("Error fetching activities");
    }
    finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchActivity();
    fetchUsers(); // Fetch users when component mounts
  }, []);

  // Close suggestions when clicking outside
  useEffect(() => {
    const handleClickOutside = (event: MouseEvent) => {
      if (showSuggestions) {
        setShowSuggestions(false);
      }
    };

    document.addEventListener('click', handleClickOutside);
    return () => {
      document.removeEventListener('click', handleClickOutside);
    };
  }, [showSuggestions]);

  const getCoordinates = async (address: string) => {
    const encodedAddress = encodeURIComponent(address);
    const url = `https://maps.googleapis.com/maps/api/geocode/json?address=${encodedAddress}&key=${import.meta.env.VITE_GOOGLE_MAPS_API_KEY}`;

    try {
      const response = await fetch(url);
      if (!response.ok) {
        throw new Error(`HTTP error! status: ${response.status}`);
      }
      const data = await response.json();
      if (data.status === "OK") {
        const location = data.results[0].geometry.location;
        return { lat: location.lat, lng: location.lng };
      } else {
        message.error(t("An error occurred while fetching location"));
        return null;
      }
    } catch (err) {
      message.error(t("An error occurred while fetching location"));
      return null;
    }
  };

  const getAddressSuggestions = async (input: string) => {
    if (input.length < 3) {
      setAddressSuggestions([]);
      setShowSuggestions(false);
      return;
    }

    try {
      const encodedInput = encodeURIComponent(input);
      const url = `${BASE_URL}/api/v1/places/autocomplete?input=${encodedInput}`;
      
      const response = await fetch(url);
      if (!response.ok) {
        throw new Error(`HTTP error! status: ${response.status}`);
      }
      
      const data = await response.json();
      if (data.status === "OK") {
        setAddressSuggestions(data.predictions || []);
        setShowSuggestions(true);
      } else {
        setAddressSuggestions([]);
        setShowSuggestions(false);
      }
    } catch (err) {
      console.error("Error fetching address suggestions:", err);
      setAddressSuggestions([]);
      setShowSuggestions(false);
    }
  };

  const handleAddressSelect = (suggestion: any) => {
    const selectedAddress = suggestion.description;
    setSelectedLocation(selectedAddress);
    form.setFieldsValue({ location: selectedAddress });
    setShowSuggestions(false);
    setAddressSuggestions([]);
  };
const joinActivity = async (activityId:string) =>{
  try {
    // Fix 1: Wrong URL structure
    const endpoint = `${url}/${activityId}/join`;
    
    // Fix 2: authHeader is a function, need to call it
    const token = authHeader();
    
    console.log('Join endpoint:', endpoint);
    console.log('Token:', token ? 'Present' : 'Missing');
    
    const headers: Record<string, string> = {
      "Content-Type": "application/json",
    };
    
    // Fix 3: Only add Authorization if token exists
    if (token && typeof token === 'string' && token.trim()) {
      headers.Authorization = token; // authHeader() already includes "Bearer "
    }
    
    
    const response = await fetch(endpoint, {
      method: "POST",
      headers
    });
    
    if (!response.ok) {
      message.error("Error joining activity");
      return { success: false, alreadyJoined: false };
    }
    
    // Handle the boolean response from backend
    const result = await response.json().catch(() => false);
    
    if (typeof result === 'boolean') {
      if (result === false) {
        // User is already joined
        message.info(t("already_joined_activity"));
        setJoined(true);
        return { success: true, alreadyJoined: true };
      } else {
        // Successfully joined
        message.success(t("successfully_joined_activity"));
        // Refresh activities to update participant count
        fetchActivity();
        return { success: true, alreadyJoined: false };
      }
    }
    
    // Fallback for other response types
    setJoined(true);
    // Refresh activities to update participant count
    fetchActivity();
    return { success: true, alreadyJoined: false };
    
  } catch (error) {
    console.error('Join activity error:', error);
    message.error("Error joining activity");
    return { success: false, alreadyJoined: false };
  }
}
  const handleCreateActivity = async (values: ActivityForm) => {
    setLoading(true);
    const locationToUse = selectedLocation || values.location;
    const location = await getCoordinates(locationToUse);

    if (!location) {
      message.error("Error fetching location of the activity");
      setLoading(false);
      return;
    }

    const activityData = {
      ...values,
      ownerId: user.id,
      location: locationToUse,
      latitude: location.lat,
      longitude: location.lng,
      selectedPeople: selectedPeople, // Include selected people in the activity data
    };

    console.log('Creating activity with data:', {
      ...activityData,
      selectedPeopleCount: selectedPeople.length,
      selectedPeopleNames: selectedPeople.map(id => people.find(p => p.id === id)?.name)
    });

    try {
      const token = authHeader();
      console.log('Create activity endpoint:', url);
      console.log('Token:', token ? 'Present' : 'Missing');
      
      const headers: Record<string, string> = {
        "Content-Type": "application/json",
      };
      
      if (token && typeof token === 'string' && token.trim()) {
        headers.Authorization = token;
      }
      
      const response = await fetch(url, {
        method: "POST",
        headers,
        body: JSON.stringify(activityData),
      });

      if (response.ok) {
        const createdActivity: Activity = await response.json();
        setActivities((prevActivities) => [...prevActivities, createdActivity]);
        message.success("Activity created successfully");
        setIsFormModalVisible(false);
        setSelectedLocation("");
        setAddressSuggestions([]);
        setShowSuggestions(false);
        setSelectedPeople([]);
        form.resetFields();
      } else {
        message.error("Error creating activity");
      }
    } catch (error) {
      message.error("Error creating activity");
    } finally {
      setLoading(false);
    }
  };

  const handleFormInputChange = (
    field: keyof Activity,
    value: string | number
  ) => {
    setNewActivity((prev) => ({
      ...prev,
      [field]: value,
    }));
  };

  const handleCardClick = (activity: Activity) => {
    setSelectedActivity(activity);
    setIsModalVisible(true);
  };

  const handleCardKeyDown = (
    event: React.KeyboardEvent,
    activity: Activity
  ) => {
    if (event.key === "Enter" || event.key === " ") {
      event.preventDefault();
      handleCardClick(activity);
    }
  };

  const handlerOpenModal = (activity: Activity) => {
    setIsModalVisible(true);
    setSelectedActivity(activity);
  };

  const handleConfirmJoin = async (activity: Activity) => {
    setIsModalVisible(false);
    const result = await joinActivity(activity.id);
    
    // Check if the join operation was successful
    if (!result?.success) return;
    setJoined(true);
    // Navigate to chat regardless of whether user was already joined or newly joined
    // This allows users who are already joined to access the chat
    navigate(`/chat/${activity.id}`);
  };

  const handleCancel = () => {
    setIsModalVisible(false);
  };

  const processDateTime = (datetime: string) => {
    const date = dayjs(datetime).format("DD/MM/YYYY"); // Get date
    const time = dayjs(datetime).format("HH:mm"); // Get time
    console.log(user.id);
    return { date, time };
  };
  console.log(selectedActivity, "selectedActivity id here");

  const items = filteredActivities.map((activity, index) => {
    const { date, time } = processDateTime(activity.dateTime);
    return (
      <div
        className="card"
        key={activity.id}
        // onClick={() => handleCardClick(activity.name)}
        // onKeyDown={(event) => handleCardKeyDown(event, activity.name)}
        tabIndex={0}
        role="button"
      >
        <Card
          title={<div className="card-title">{activity.title}</div>}
          extra={
            <div>
              <UserOutline /> {activity.participantCount || 0}/{activity.peopleLimit}
            </div>
          }
          // onBodyClick={onBodyClick}
          // onHeaderClick={onHeaderClick}
          style={{ borderRadius: "16px" }}
        >
          <div className="card-content">
            <p>{activity.description}</p>
          </div>
          <div className="card-footer">
            <div className="date-container">
              <div>
                <EnvironmentOutline />
                <span>{activity.location}</span>
              </div>
              <div>
                <CalendarOutline />
                <span>{date}</span>
              </div>
              <div>
                <ClockCircleOutline />
                <span>{time}</span>
              </div>
            </div>
            <div className="buttons-container">
              {/* {user.id == activity.ownerId && (
                <>
                  <Button
                    color="danger"
                    onClick={() => {
                      // Toast.show("点击了底部按钮");
                    }}
                  >
                    {t("delete_button")}
                  </Button>
                  <Button
                    color="danger"
                    onClick={() => {
                      // Toast.show("点击了底部按钮");
                    }}
                  >
                    {t("edit_button")}
                  </Button>
                </>
              )} */}
            </div>
            {!join && 
            <Button
              color="primary"
              onClick={() => {
                handlerOpenModal(activity);
              }}
            >
              {t("global:join")}
            </Button>
            }
            {
              join &&
              <Button

              >
                Chat
              </Button>
            }
          </div>
        </Card>
      </div>
    );
  });

  const joinActivityModal = () => {
    return (
      <Modal
        visible={isModalVisible}
        content={t("join_activity_message", {
          name: selectedActivity?.title,
        })}
        closeOnMaskClick={true}
        onClose={handleCancel}
        actions={[
          { key: "no", text: t("no"), onClick: handleCancel },
          {
            key: "yes",
            text: t("yes"),
            onClick: () => handleConfirmJoin(selectedActivity!),
          },
        ]}
      />
    );
  };
  
  useEffect(() => {
    const filtered = activities.filter(
      (activity) =>
        activity.title.toLowerCase().includes(searchTerm.toLowerCase()) ||
        activity.description.toLowerCase().includes(searchTerm.toLowerCase())
    );
    setFilteredActivities(filtered);
  }, [searchTerm, activities]);

  const createActivityModal = () => {
    return (
      <Modal
        visible={isFormModalVisible}
        closeOnMaskClick={true}
        onClose={() => {
          setIsFormModalVisible(false);
          setSelectedLocation("");
          setAddressSuggestions([]);
          setShowSuggestions(false);
          setSelectedPeople([]);
          form.resetFields();
        }}
        title={t("create_activity")}
        style={{
          width: "100%", // Custom width
          maxWidth: "500px", // Optional max width for responsiveness
        }}
        content={
          <Form
            form={form}
            layout="horizontal"
            mode="card"
            onFinish={handleCreateActivity}
            footer={
              <>
                <Button block type="submit" color="primary" size="large">
                  {t("global:publish")}
                </Button>
              </>
            }
          >
            <Form.Item
              name="title"
              rules={[{ required: true, message: "" }]}
              label={<EditSOutline />}
            >
              <Input
                placeholder={t("global:title")}
                name="title"
              // onChange={(value) => {
              //   setUsername(value);
              // }}
              // className="form-input"
              />
            </Form.Item>
            <Form.Item
              name="description"
              rules={[
                {
                  required: true,
                  message: "",
                },
              ]}
            >
              <TextArea
                placeholder={t("global:description")}
                maxLength={100}
                rows={2}
                showCount
                className="form-input"
              />
            </Form.Item>
            <Form.Item
              name="location"
              rules={[{ required: true, message: "" }]}
              label={<EnvironmentOutline />}
            >
              <div style={{ position: 'relative' }}>
                <Input 
                  placeholder={t("global:location")} 
                  name="location"
                  value={selectedLocation}
                  onChange={(value) => {
                    setSelectedLocation(value);
                    getAddressSuggestions(value);
                  }}
                  onFocus={() => {
                    if (addressSuggestions.length > 0) {
                      setShowSuggestions(true);
                    }
                  }}
                />
                {showSuggestions && addressSuggestions.length > 0 && (
                  <div style={{
                    position: 'absolute',
                    top: '100%',
                    left: 0,
                    right: 0,
                    backgroundColor: 'white',
                    border: '1px solid #d9d9d9',
                    borderRadius: '6px',
                    boxShadow: '0 2px 8px rgba(0, 0, 0, 0.1)',
                    zIndex: 1000,
                    maxHeight: '200px',
                    overflowY: 'auto'
                  }}>
                    <List>
                      {addressSuggestions.map((suggestion, index) => (
                        <List.Item
                          key={suggestion.place_id || index}
                          onClick={() => handleAddressSelect(suggestion)}
                          style={{
                            cursor: 'pointer',
                            padding: '8px 16px',
                            fontSize: '14px'
                          }}
                        >
                          <div style={{
                            display: 'flex',
                            alignItems: 'center',
                            gap: '8px'
                          }}>
                            <EnvironmentOutline style={{ color: '#1890ff' }} />
                            <span>{suggestion.description}</span>
                          </div>
                        </List.Item>
                      ))}
                    </List>
                  </div>
                )}
              </div>
            </Form.Item>
            <Form.Item
              name="select-people"
              label={<UserOutline />}
            >
              <div>
                <div style={{ marginBottom: '8px', fontSize: '14px', color: '#666' }}>
                  {t("global:select_people")} ({selectedPeople.length} selected)
                </div>
                <Selector
                  multiple
                  value={selectedPeople}
                  onChange={setSelectedPeople}
                  options={people.map(person => ({
                    label: (
                      <div style={{ 
                        display: 'flex', 
                        flexDirection: 'column', 
                        alignItems: 'flex-start',
                        padding: '4px 0'
                      }}>
                        <div style={{ fontWeight: 'bold', fontSize: '14px' }}>
                          {person.name}
                        </div>
                        <div style={{ fontSize: '12px', color: '#888' }}>
                          {person.email}
                        </div>
                      </div>
                    ),
                    value: person.id,
                  }))}
                  style={{
                    '--border': '1px solid #e5e5e5',
                    '--border-radius': '8px',
                    '--padding': '12px',
                  }}
                />
                {selectedPeople.length > 0 && (
                  <div style={{ 
                    marginTop: '8px', 
                    padding: '8px', 
                    backgroundColor: '#f0f9ff',
                    borderRadius: '6px',
                    fontSize: '12px'
                  }}>
                    <strong>Selected:</strong> {
                      selectedPeople.map(id => 
                        people.find(p => p.id === id)?.name
                      ).join(', ')
                    }
                  </div>
                )}
              </div>
            </Form.Item>
            <Form.Item
              name="dateTime"
              rules={[
                {
                  required: true,
                  message: "",
                },
              ]}
              trigger="onConfirm"
              onClick={(e, datePickerRef: RefObject<DatePickerRef>) => {
                datePickerRef.current?.open();
              }}
              label={<CalendarOutline />}
            >
              <DatePicker
                precision="minute"
                cancelText={t("global:cancel")}
                confirmText={t("global:confirm")}
              >
                {(value) =>
                  value
                    ? dayjs(value).format(dateFormat + " " + timeFormat)
                    : ""
                }
              </DatePicker>
            </Form.Item>
            
            <Form.Item
              initialValue={2}
              rules={[
                {
                  required: true,
                  message: "",
                },
              ]}
              name="peopleLimit"
              label={<UserOutline />}
            >
              <Stepper min={2} />
            </Form.Item>
          </Form>
        }
      />
    );
  };
  // useEffect(() => {
  //   fetch2();
  // }, [fromDate,toDate])
  

  return (
    <div className="activities-container">
      <div className="search-bar">
        <SearchBar
          placeholder={t("search_placeholder")}
          onChange={(value) => setSearchTerm(value)}
        />
        <Button color="primary" onClick={() => setIsFormModalVisible(true)}>
          <AddOutline />
        </Button>
      </div>
      <>
        {/* <Button className="fromDateBtn" onClick={()=>setOpen(true)} >
          {fromDate? dayjs(fromDate).format("YYYY-MM-DD") : t("From date")}
            <DatePicker
            visible={open}
            precision="day"
            cancelText={t("global:cancel")}
            confirmText={t("global:confirm")}
            onCancel={()=>setOpen(false)}
            onConfirm={(date)=>setFromDate(date as Date)}
            >
            </DatePicker>
        </Button>
        <Button className="toDateBtn" onClick={()=>setOpen(true)}>
          {toDate? dayjs(toDate).format("YYYY-MM-DD"): t("To date")}
          <DatePicker 
            precision="day"
            cancelText={t("global:cancel")}
            confirmText={t("global:confirm")}
            visible={open}
            onCancel={()=>setOpen(false)}
            onConfirm={(date2)=>setToDate(date2 as Date)}>  
          </DatePicker>
        </Button> */}

      </>
      <Divider />
      <div className="scroll">
        {loading && <SpinLoading />}
        <div className="card-activities">{items}</div>
      </div>
      {isModalVisible && joinActivityModal()}
      <div className="create-activity-modal">
        {isFormModalVisible && createActivityModal()}
      </div>
    </div>
  );
};

export default ActivitiesMobilePage;
