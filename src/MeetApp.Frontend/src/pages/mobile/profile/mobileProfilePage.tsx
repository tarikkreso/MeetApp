import { isMobile } from "react-device-detect";
if (isMobile) import("./mobileProfilePageStyles.css");

import React, { useState } from "react";
import { useAuthUser, useSignOut } from "react-auth-kit";
import { useTranslation } from "react-i18next";
import { Divider, Form, Input, Avatar, Button, Modal } from "antd-mobile";
import { BASE_URL } from "../../../configs/GeneralApiType";
import { useNavigate } from "react-router-dom";
import { notification } from "antd";
const url = `${BASE_URL}/api/v1/users`;
interface RegisterForm {
  email: string;
  password: string;
  userType: string;
  city: string;
  profilePicture: string;
  businessName: string;
  businessAddress: string;
  businessCategory: string;
  cif: string;
  googleMapsUrl: string;
}

interface Profile {
  name: string;
  email: string;
  password: string;
  userType: string;
  city: string;
  profilePicture: string;
  businessName: string;
  businessAddress: string;
  businessCategory: string;
  cif: string;
  googleMapsUrl: string;
}

const MobileProfilePage = () => {
  const { t } = useTranslation("profilepage");
  const user = useAuthUser()()?.user;
  const [form] = Form.useForm<RegisterForm>();
  const [isEditing, setIsEditing] = useState(false);
  const [isDeleted, setDeleted]=useState(false);

  const [profilePicture, setProfilePicture] = useState<string>(
    user?.profilePicture
  );
  const signOut = useSignOut();
  const navigate = useNavigate();

  const handleEdit = () => {
    setIsEditing(!isEditing);
    // if (isEditing) {
    //   form.resetFields();
    // }
  };

  const handleSave = () => {
    // form.submit();
    // form.resetFields();
    setProfilePicture(form.getFieldValue("profilePicture"));
    console.log("save");
    setIsEditing(!isEditing);
  };

  const handleCancel = () => {
    form.resetFields();
    setProfilePicture(user?.profilePicture);
    console.log("cancel");
    setIsEditing(!isEditing);
  };

  const handleLogout = () => {
    signOut();
    navigate("/login");
  };

  const handleDelete = async (id: string) => {
    Modal.confirm({
      title: t("account_delete_message"),
      confirmText: t("accept_delete_account_button"), // Custom text for OK button
      cancelText: t("cancel_delete_account_button"), // Custom text for Cancel button
      onConfirm: async () => {
        try {
          const r=await fetch(`${url}/${id}`, {
            method: "DELETE",
            headers: {
              "Content-Type":"application/json"
            }
          });
          if(r.ok){
            console.log("User deleted.")
            setDeleted(true);
          }
        } catch (error) {
          console.log(error, ": Server Error.")
        }
        handleLogout();
      },
    });
  };

  return (
    <div className="profile-container">
      <div className="profile-photo">
        <Avatar src={profilePicture} className="avatar-size" />
      </div>
      <div className="button-group">
        <Button
          color="primary"
          className="profile-button"
          style={{ margin: 0 }}
          onClick={isEditing ? handleSave : handleEdit}
        >
          {isEditing ? t("save_button") : t("edit_button")}
        </Button>
        <Button
          className="profile-button"
          color="danger"
          fill={isEditing ? "outline" : "solid"}
          onClick={isEditing ? handleCancel : () => handleDelete(user?.id)}
        >
          {isEditing ? t("cancel_button") : t("delete_button")}
        </Button>
      </div>
      <div className="info-fields">
        <Form form={form} layout="vertical" disabled={!isEditing}>
          <Form.Item label={t("name")} name="name" initialValue={user?.name}>
            <Input className="form-input" />
          </Form.Item>
          <Form.Item label={t("email")} name="email" initialValue={user?.email}>
            <Input className="form-input" />
          </Form.Item>

          <Form.Item label={t("city")} name="city" initialValue={user?.city}>
            <Input className="form-input" />
          </Form.Item>

          <Form.Item
            label={t("profile_picture")}
            name="profilePicture"
            initialValue={user?.profilePicture}
          >
            <Input
              onChange={(value) => {
                setProfilePicture(value);
              }}
              className="form-input"
            />
          </Form.Item>
        </Form>
      </div>
    </div>
  );
};

export default MobileProfilePage;
