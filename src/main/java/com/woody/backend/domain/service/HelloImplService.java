package com.woody.backend.domain.service;

import java.util.List;

import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.stereotype.Service;

import com.woody.backend.domain.dto.HelloDto;
import com.woody.backend.domain.service.interfaces.HelloService;
import com.woody.backend.infraestructure.repository.interfaces.HelloRepository;

@Service
public class HelloImplService implements HelloService {
    @Autowired
    private HelloRepository helloRepository;

    public HelloImplService(){}

    @Override
    public List<HelloDto> getHello(){
        return this.helloRepository.get().stream().map(h -> new HelloDto(h.getHello())).toList();
    }
}
