import React, { useState, useEffect } from 'react';
import { Form, InputNumber, Button, Card, Select, message, Steps, Result, Input, Popconfirm } from 'antd';
import { SolutionOutlined, BankOutlined, SmileOutlined, ArrowLeftOutlined, LogoutOutlined } from '@ant-design/icons';
import axiosClient from '../api/axiosClient';
import { jwtDecode } from "jwt-decode";
import { useNavigate } from 'react-router-dom';

const { Option } = Select;

const LoanRegistration = () => {
  const [loading, setLoading] = useState(false);
  const [loanResult, setLoanResult] = useState(null);
  const [userId, setUserId] = useState(null);
  const navigate = useNavigate();

  // Xử lý đăng xuất
  const handleLogout = () => {
    localStorage.clear();
    navigate('/login');
  };

  useEffect(() => {
    const token = localStorage.getItem('token');
    if (!token) {
      navigate('/login');
      return;
    }
    try {
      const decoded = jwtDecode(token);
      const idKey = Object.keys(decoded).find(key => key.includes('nameidentifier'));
      const id = decoded[idKey] || decoded.nameid || decoded.sub || decoded.Id;
      setUserId(parseInt(id));
    } catch (e) {
      navigate('/login');
    }
  }, [navigate]);

  const onFinish = async (values) => {
    setLoading(true);
    try {
      const profileData = {
        userId: parseInt(userId),
        monthlyIncome: parseFloat(values.monthlyIncome),
        existingDebt: parseFloat(values.existingDebt),
        employmentStatus: values.employmentStatus,
        hasCollateral: String(values.hasCollateral) === 'true'
      };

      try {
        await axiosClient.post('/FinancialProfile', profileData);
      } catch (err) {
        if (err.response && err.response.status === 409) {
          await axiosClient.put(`/FinancialProfile/${userId}`, profileData);
        }
      }

      // Gửi đơn vay
      await axiosClient.post('/Loan', {
        userId: parseInt(userId),
        amount: values.amount,
        purpose: values.purpose
      });

      setLoanResult('Pending'); 
      message.success('Đã gửi hồ sơ thành công!');
    } catch (error) {
      message.error('Lỗi hệ thống! Vui lòng thử lại.');
    }
    setLoading(false);
  };

  if (loanResult) {
    return (
      <div style={{ padding: '40px', background: '#f0f2f5', minHeight: '100vh', display: 'flex', justifyContent: 'center', alignItems: 'center' }}>
        <Card style={{ maxWidth: 600, width: '100%', borderRadius: 12 }}>
            <Result
            status="info"
            title="HỒ SƠ ĐANG CHỜ THẨM ĐỊNH"
            subTitle="Hệ thống đã ghi nhận đơn vay. Admin sẽ xem xét và phản hồi sớm nhất."
            extra={[
                <Button type="primary" key="console" onClick={() => navigate('/dashboard')}>
                Về Dashboard theo dõi
                </Button>,
                <Button key="buy" onClick={() => window.location.reload()}>
                Tạo đơn khác
                </Button>,
            ]}
            />
        </Card>
      </div>
    );
  }

  return (
    <div style={{ background: '#f0f2f5', minHeight: '100vh', paddingBottom: 40 }}>
      
      {/* --- HEADER ĐIỀU HƯỚNG --- */}
      <div style={{ background: '#fff', padding: '15px 40px', display: 'flex', justifyContent: 'space-between', alignItems: 'center', boxShadow: '0 2px 8px rgba(0,0,0,0.05)' }}>
          <Button icon={<ArrowLeftOutlined />} onClick={() => navigate('/dashboard')}>
            Quay lại Dashboard
          </Button>
          <div style={{ fontWeight: 'bold', fontSize: 18, color: '#1890ff' }}>EasyCredit Form</div>
          <Button danger type="text" icon={<LogoutOutlined />} onClick={handleLogout}>
            Đăng xuất
          </Button>
      </div>

      {/* --- FORM ĐĂNG KÝ --- */}
      <div style={{ maxWidth: 700, margin: '40px auto' }}>
        <Card title="💸 Đăng ký vay vốn" bordered={false} style={{ borderRadius: 12, boxShadow: '0 4px 12px rgba(0,0,0,0.05)' }}>
            <Steps items={[{ title: 'Đăng nhập', status: 'finish', icon: <SmileOutlined /> }, { title: 'Điền hồ sơ', status: 'process', icon: <SolutionOutlined /> }, { title: 'Nhận kết quả', status: 'wait', icon: <BankOutlined /> }]} style={{ marginBottom: 30 }} />
            
            <Form layout="vertical" onFinish={onFinish}>
            <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 20 }}>
                <Form.Item label="Số tiền vay (VNĐ)" name="amount" rules={[{ required: true, message: 'Vui lòng nhập số tiền' }]}>
                <InputNumber style={{ width: '100%' }} formatter={value => `${value}`.replace(/\B(?=(\d{3})+(?!\d))/g, ',')} min={1000000} step={1000000} />
                </Form.Item>
                <Form.Item label="Mục đích vay" name="purpose" rules={[{ required: true, message: 'Nhập mục đích vay' }]}>
                <Input placeholder="Mua xe, kinh doanh..." />
                </Form.Item>
            </div>

            <div style={{ background: '#fafafa', padding: 20, borderRadius: 8, marginBottom: 20 }}>
                <h4 style={{ marginTop: 0 }}>📋 Hồ sơ tài chính (AI Scoring)</h4>
                <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 20 }}>
                    <Form.Item label="Thu nhập hàng tháng" name="monthlyIncome" rules={[{ required: true }]}>
                    <InputNumber style={{ width: '100%' }} formatter={value => `${value}`.replace(/\B(?=(\d{3})+(?!\d))/g, ',')} />
                    </Form.Item>
                    <Form.Item label="Tổng nợ hiện tại" name="existingDebt" rules={[{ required: true }]}>
                    <InputNumber style={{ width: '100%' }} formatter={value => `${value}`.replace(/\B(?=(\d{3})+(?!\d))/g, ',')} />
                    </Form.Item>
                    <Form.Item label="Nghề nghiệp" name="employmentStatus" initialValue="Employed">
                    <Select><Option value="Employed">Đi làm hưởng lương</Option><Option value="SelfEmployed">Kinh doanh tự do</Option><Option value="Unemployed">Khác</Option></Select>
                    </Form.Item>
                    <Form.Item label="Tài sản đảm bảo" name="hasCollateral" initialValue="false">
                    <Select><Option value="true">Có (Nhà/Xe)</Option><Option value="false">Không có</Option></Select>
                    </Form.Item>
                </div>
            </div>

            <div style={{ display: 'flex', gap: 10 }}>
                <Button size="large" onClick={() => navigate('/dashboard')} style={{ flex: 1 }}>Hủy bỏ</Button>
                <Button type="primary" htmlType="submit" size="large" loading={loading} style={{ flex: 2, background: '#1890ff' }}>
                    GỬI HỒ SƠ THẨM ĐỊNH
                </Button>
            </div>
            </Form>
        </Card>
      </div>
    </div>
  );
};

export default LoanRegistration;