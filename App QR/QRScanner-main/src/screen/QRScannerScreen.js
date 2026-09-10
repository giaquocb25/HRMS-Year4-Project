import axios from 'axios';
import React, {useRef, useState} from 'react';
import {Alert, StyleSheet, Text} from 'react-native';
import QRCodeScanner from 'react-native-qrcode-scanner';

export default function QRScannerScreen({route}) {
  const qr = useRef(null);
  const [status, setStatus] = useState('Sẵn sàng quét mã nhân viên');
  const [isSubmitting, setIsSubmitting] = useState(false);

  const reactivate = () => {
    setTimeout(() => qr.current?.reactivate(), 1200);
  };

  const onSuccess = async event => {
    if (isSubmitting) {
      return;
    }

    const employeeId = Number.parseInt(String(event.data || '').trim(), 10);
    if (!Number.isInteger(employeeId) || employeeId <= 0) {
      Alert.alert('Mã không hợp lệ', 'Mã QR không chứa mã nhân viên hợp lệ.');
      reactivate();
      return;
    }

    setIsSubmitting(true);
    setStatus(`Đang gửi chấm công cho nhân viên #${employeeId}...`);
    const url = `http://${route.params.ip}:${
      route.params.port || 8080
    }/timekeeping/`;

    try {
      const response = await axios.post(
        url,
        {
          employeeId,
          token: route.params.token,
          eventTime: new Date().toISOString(),
          externalEventId: `mobile-${employeeId}-${Date.now()}`,
          deviceName: 'HRMS Android QR',
        },
        {timeout: 8000},
      );
      setStatus(
        response.data?.message || `Đã chấm công nhân viên #${employeeId}.`,
      );
    } catch (error) {
      const message =
        error.response?.data?.message || 'Không thể kết nối máy quản lý.';
      setStatus(message);
      Alert.alert('Chấm công chưa thành công', message);
    } finally {
      setIsSubmitting(false);
      reactivate();
    }
  };

  return (
    <QRCodeScanner
      ref={node => {
        qr.current = node;
      }}
      onRead={onSuccess}
      topContent={<Text style={styles.centerText}>{status}</Text>}
    />
  );
}

const styles = StyleSheet.create({
  centerText: {
    flex: 1,
    fontSize: 18,
    padding: 32,
    color: '#374151',
    textAlign: 'center',
  },
});
